using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vigia.Domain.Tags;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Tags on checks and tag selectors on rules.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TagsTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Checks_carry_user_tags_flags_and_the_plugin_system_tag()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = await CreateCheckAsync(client, new Dictionary<string, string?> { ["env"] = "prod", ["critical"] = null });

        var tags = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct)).GetProperty("tags");

        Assert.Equal("prod", tags.GetProperty("env").GetString());
        Assert.Equal(JsonValueKind.Null, tags.GetProperty("critical").ValueKind);
        Assert.Equal("vigia.check.http", tags.GetProperty("vigia:plugin").GetString());
    }

    [Fact]
    public async Task Updating_tags_keeps_system_tags()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = await CreateCheckAsync(client, new Dictionary<string, string?> { ["env"] = "prod" });

        (await client.PutAsJsonAsync($"/api/v1/checks/{slug}", new { config = new { url = "http://127.0.0.1:1/" }, tags = new { team = "platform" } }, Ct)).EnsureSuccessStatusCode();
        var tags = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct)).GetProperty("tags");

        Assert.False(tags.TryGetProperty("env", out _));
        Assert.Equal("platform", tags.GetProperty("team").GetString());
        Assert.Equal("vigia.check.http", tags.GetProperty("vigia:plugin").GetString());
    }

    [Fact]
    public async Task Assignable_system_tags_are_written_with_user_tags_and_replaced_like_them()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = await CreateCheckAsync(client, new Dictionary<string, string?> { ["vigia:external"] = null, ["vendor"] = "sabre" });

        var created = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct)).GetProperty("tags");
        (await client.PutAsJsonAsync($"/api/v1/checks/{slug}", new { config = new { url = "http://127.0.0.1:1/" }, tags = new { vendor = "sabre" } }, Ct)).EnsureSuccessStatusCode();
        var updated = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct)).GetProperty("tags");

        Assert.Equal(JsonValueKind.Null, created.GetProperty("vigia:external").ValueKind);
        Assert.False(updated.TryGetProperty("vigia:external", out _));
        Assert.Equal("vigia.check.http", updated.GetProperty("vigia:plugin").GetString());
    }

    [Fact]
    public async Task Assignable_tags_follow_their_definition()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var flagWithValue = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = $"tags-{Guid.NewGuid():N}"[..20],
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            tags = new Dictionary<string, string?> { ["vigia:external"] = "yes" },
        }, Ct);
        var wrongEntity = await client.PostAsJsonAsync("/api/v1/workers", new
        {
            slug = $"tagw-{Guid.NewGuid():N}"[..20],
            tags = new Dictionary<string, string?> { ["vigia:external"] = null },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, flagWithValue.StatusCode);
        Assert.Contains("flag", await flagWithValue.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.BadRequest, wrongEntity.StatusCode);
        Assert.Contains("does not apply to workers", await wrongEntity.Content.ReadAsStringAsync(Ct));
    }

    [Theory]
    [InlineData("vigia:plugin", "evil")]
    [InlineData("vigia:nope", "x")]
    [InlineData("vigia", "x")]
    [InlineData("Env", "prod")]
    [InlineData("1env", "prod")]
    [InlineData("env var", "prod")]
    public async Task Invalid_or_reserved_tags_are_rejected(string key, string value)
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = $"tags-{Guid.NewGuid():N}"[..20],
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            tags = new Dictionary<string, string?> { [key] = value },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors").TryGetProperty("tags", out _));
    }

    [Fact]
    public async Task Selector_is_returned_in_the_shape_it_was_written()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = $"sel-{Guid.NewGuid():N}"[..20];
        var body = JsonNode("""
            { "selector": { "region": ["eu", "us"], "network": "vpc", "critical": null }, "when": { "outcome": "down" } }
            """);
        body["slug"] = slug;
        (await client.PostAsJsonAsync("/api/v1/rules", body, Ct)).EnsureSuccessStatusCode();

        var selector = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/rules/{slug}", Ct)).GetProperty("selector");

        Assert.Equal(["eu", "us"], selector.GetProperty("region").EnumerateArray().Select(v => v.GetString()));
        Assert.Equal("vpc", selector.GetProperty("network").GetString());
        Assert.Equal(JsonValueKind.Null, selector.GetProperty("critical").ValueKind);
    }

    [Theory]
    [InlineData("""{ "vigia:nope": "x" }""", "selector")]
    [InlineData("""{ "Region": "eu" }""", "selector")]
    [InlineData("""{ "region": 5 }""", "selector.region")]
    [InlineData("""{ "region": ["eu", 5] }""", "selector.region")]
    public async Task Invalid_selectors_are_rejected(string selector, string field)
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var body = JsonNode($$"""{ "selector": {{selector}}, "when": { "outcome": "down" } }""");
        body["slug"] = $"bad-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/api/v1/rules", body, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _), $"Expected an error on '{field}', got {errors}.");
    }

    [Fact]
    public void Selector_ands_keys_ors_values_and_null_means_present()
    {
        var selector = TagSelector.Create(new Dictionary<string, IReadOnlyList<string>>
        {
            ["region"] = ["eu", "us"],
            ["network"] = ["vpc"],
            ["critical"] = [],
        });

        Assert.True(selector.Matches(Tags(("region", "eu"), ("network", "vpc"), ("critical", null))));
        Assert.True(selector.Matches(Tags(("region", "us"), ("network", "vpc"), ("critical", "yes"))));
        Assert.False(selector.Matches(Tags(("region", "asia"), ("network", "vpc"), ("critical", null))));
        Assert.False(selector.Matches(Tags(("region", "eu"), ("network", "public"), ("critical", null))));
        Assert.False(selector.Matches(Tags(("region", "eu"), ("network", "vpc"))));
        Assert.False(selector.Matches(Tags(("region", null), ("network", "vpc"), ("critical", null))));
        Assert.True(TagSelector.Any.Matches(Tags()));
    }

    private static Dictionary<string, string?> Tags(params (string Key, string? Value)[] tags)
    {
        return tags.ToDictionary(t => t.Key, t => t.Value);
    }

    private static System.Text.Json.Nodes.JsonObject JsonNode(string json)
    {
        return System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
    }

    private static async Task<string> CreateCheckAsync(HttpClient client, IReadOnlyDictionary<string, string?> tags)
    {
        var slug = $"tags-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            tags,
        }, Ct);
        response.EnsureSuccessStatusCode();
        return slug;
    }
}
