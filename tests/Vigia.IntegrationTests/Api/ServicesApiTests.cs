using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Services: check selection, rule coverage, partitioning and the dependency graph.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ServicesApiTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Service_selects_checks_by_tag_and_reports_uncovered_ones()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("svc");
        var covered = await CreateCheckAsync(client, new() { ["service"] = tag, ["region"] = "eu-west" });
        var uncovered = await CreateCheckAsync(client, new() { ["service"] = tag, ["region"] = "us-east" });
        await CreateCheckAsync(client, new() { ["service"] = Unique("other") });
        (await client.PostAsJsonAsync("/api/v1/rules", new { slug = Unique("rule"), check = covered, when = new { outcome = "down" } }, Ct)).EnsureSuccessStatusCode();

        var service = await CreateServiceAsync(client, new
        {
            tags = new Dictionary<string, string?> { ["team"] = "platform", ["vigia:external"] = null },
            checks = new { service = tag },
            partitionBy = "region",
        });

        Assert.Equal(new[] { covered, uncovered }.Order(), Strings(service, "matchedChecks"));
        Assert.Equal([uncovered], Strings(service, "uncoveredChecks"));
        Assert.Equal("region", service.GetProperty("partitionBy").GetString());
        Assert.Equal(JsonValueKind.Null, service.GetProperty("tags").GetProperty("vigia:external").ValueKind);
        Assert.Equal(tag, service.GetProperty("checks").GetProperty("service").GetString());
    }

    [Fact]
    public async Task Without_a_selector_a_service_has_no_checks()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        await CreateCheckAsync(client, new() { ["service"] = Unique("x") });

        var service = await CreateServiceAsync(client, new { });

        Assert.Equal(JsonValueKind.Null, service.GetProperty("checks").ValueKind);
        Assert.Empty(Strings(service, "matchedChecks"));
    }

    [Fact]
    public async Task Dependencies_round_trip_with_modes()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var sabre = Slug(await CreateServiceAsync(client, new { tags = new Dictionary<string, string?> { ["vigia:external"] = null } }));
        var dns = Slug(await CreateServiceAsync(client, new { }));

        var booking = await CreateServiceAsync(client, new { dependsOn = new object[] { new { service = sabre }, new { service = dns, mode = "soft" } } });

        var deps = booking.GetProperty("dependsOn").EnumerateArray().ToDictionary(d => d.GetProperty("service").GetString()!, d => d.GetProperty("mode").GetString());
        Assert.Equal("blocking", deps[sabre]);
        Assert.Equal("soft", deps[dns]);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("self")]
    [InlineData("mode")]
    [InlineData("twice")]
    public async Task Invalid_dependencies_are_rejected(string kind)
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var other = Slug(await CreateServiceAsync(client, new { }));
        var slug = Unique("svc");
        object[] dependsOn = kind switch
        {
            "unknown" => [new { service = Unique("ghost") }],
            "self" => [new { service = slug }],
            "mode" => [new { service = other, mode = "sometimes" }],
            _ => [new { service = other }, new { service = other, mode = "soft" }],
        };

        // Self-dependency needs the service to exist first.
        if (kind == "self")
        {
            await CreateServiceAsync(client, new { }, slug);
        }

        var response = kind == "self"
            ? await client.PutAsJsonAsync($"/api/v1/services/{slug}", new { dependsOn }, Ct)
            : await client.PostAsJsonAsync("/api/v1/services", new { slug, dependsOn }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors").TryGetProperty("dependsOn", out _));
    }

    [Fact]
    public async Task Cycles_are_rejected_with_the_path()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var c = Slug(await CreateServiceAsync(client, new { }));
        var b = Slug(await CreateServiceAsync(client, new { dependsOn = new[] { new { service = c } } }));
        var a = Slug(await CreateServiceAsync(client, new { dependsOn = new[] { new { service = b } } }));

        var response = await client.PutAsJsonAsync($"/api/v1/services/{c}", new { dependsOn = new[] { new { service = a } } }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains($"{c} -> {a} -> {b} -> {c}", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Deleting_a_service_removes_edges_pointing_at_it()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var provider = Slug(await CreateServiceAsync(client, new { }));
        var dependent = Slug(await CreateServiceAsync(client, new { dependsOn = new[] { new { service = provider } } }));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/services/{provider}", Ct)).StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>($"/api/v1/services/{dependent}", Ct);
        Assert.Empty(after.GetProperty("dependsOn").EnumerateArray());
    }

    [Fact]
    public async Task Update_replaces_everything_and_invalid_fields_are_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var provider = Slug(await CreateServiceAsync(client, new { }));
        var slug = Slug(await CreateServiceAsync(client, new { partitionBy = "region", dependsOn = new[] { new { service = provider } } }));

        var updated = await client.PutAsJsonAsync($"/api/v1/services/{slug}", new { name = "Bookings" }, Ct);
        var badPartition = await client.PutAsJsonAsync($"/api/v1/services/{slug}", new { partitionBy = "Region Key" }, Ct);
        var duplicate = await client.PostAsJsonAsync("/api/v1/services", new { slug }, Ct);

        updated.EnsureSuccessStatusCode();
        var body = await updated.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Bookings", body.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("partitionBy").ValueKind);
        Assert.Empty(body.GetProperty("dependsOn").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, badPartition.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains(await client.GetFromJsonAsync<JsonElement[]>("/api/v1/services", Ct) ?? [], s => Slug(s) == slug);
    }

    private static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..16];
    }

    private static string Slug(JsonElement service)
    {
        return service.GetProperty("slug").GetString()!;
    }

    private static List<string> Strings(JsonElement element, string property)
    {
        return [.. element.GetProperty(property).EnumerateArray().Select(e => e.GetString()!)];
    }

    private static async Task<string> CreateCheckAsync(HttpClient client, Dictionary<string, string?> tags)
    {
        var slug = Unique("chk");
        (await client.PostAsJsonAsync("/api/v1/checks", new { slug, plugin = "vigia.check.http", config = new { url = "http://127.0.0.1:1/" }, tags }, Ct)).EnsureSuccessStatusCode();
        return slug;
    }

    private static async Task<JsonElement> CreateServiceAsync(HttpClient client, object spec, string? slug = null)
    {
        var body = JsonSerializer.SerializeToNode(spec, JsonSerializerOptions.Web)!.AsObject();
        body["slug"] = slug ?? Unique("svc");
        var response = await client.PostAsJsonAsync("/api/v1/services", body, Ct);
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}
