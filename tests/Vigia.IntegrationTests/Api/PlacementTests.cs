using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Workers;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Which workers run which checks: selectors over worker tags, placement state and per-worker assignments.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PlacementTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Without_a_selector_every_worker_runs_the_check()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var worker = await CreateEnrolledWorkerAsync(client, Unique("r"));
        var check = await CreateCheckAsync(client, workers: null);

        Assert.Contains(worker.Slug, (await PlacementAsync(client, check)).Eligible);
        Assert.Contains(check, await AssignedSlugsAsync(worker.Credential));
    }

    [Fact]
    public async Task Selector_ands_keys_and_ors_values()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (ra, rb) = (Unique("ra"), Unique("rb"));
        var a = await CreateEnrolledWorkerAsync(client, ra, new() { ["network"] = "vpc" });
        var b = await CreateEnrolledWorkerAsync(client, rb, new() { ["network"] = "vpc" });
        var c = await CreateEnrolledWorkerAsync(client, ra, new() { ["network"] = "dmz" });
        var check = await CreateCheckAsync(client, new { match = new Dictionary<string, object> { ["vigia:region"] = new[] { ra, rb }, ["network"] = "vpc" } });

        var placement = await PlacementAsync(client, check);

        Assert.Equal("ok", placement.State);
        Assert.Equal(new[] { a.Slug, b.Slug }.Order(), placement.Eligible);
        Assert.Contains(check, await AssignedSlugsAsync(a.Credential));
        Assert.Contains(check, await AssignedSlugsAsync(b.Credential));
        Assert.DoesNotContain(check, await AssignedSlugsAsync(c.Credential));
    }

    [Fact]
    public async Task No_matching_worker_is_unschedulable()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client, new { match = new { region = Unique("nowhere") } });

        var placement = await PlacementAsync(client, check);

        Assert.Equal("unschedulable", placement.State);
        Assert.Empty(placement.Eligible);
    }

    [Fact]
    public async Task Matching_workers_that_never_reported_are_offline_until_one_does()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var region = Unique("r");
        var (slug, token) = await CreateWorkerAsync(client, region);
        var check = await CreateCheckAsync(client, new { match = new Dictionary<string, string> { ["vigia:region"] = region } });

        var before = await PlacementAsync(client, check);
        await EnrollAsync(token);
        var after = await PlacementAsync(client, check);

        Assert.Equal("workers-offline", before.State);
        Assert.Equal([slug], before.Eligible);
        Assert.Empty(before.Online);
        Assert.Equal("ok", after.State);
        Assert.Equal([slug], after.Online);
    }

    [Fact]
    public async Task Webhook_plugins_run_only_on_the_control_plane()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var remote = await CreateEnrolledWorkerAsync(client, Unique("r"));

        var withMatch = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = Unique("hb"),
            plugin = "vigia.check.heartbeat",
            config = new { },
            workers = new { match = new { region = "eu" } },
        }, Ct);
        var heartbeat = Unique("hb");
        (await client.PostAsJsonAsync("/api/v1/checks", new { slug = heartbeat, plugin = "vigia.check.heartbeat", config = new { } }, Ct)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.BadRequest, withMatch.StatusCode);
        Assert.True((await withMatch.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors").TryGetProperty("workers.match", out _));
        Assert.DoesNotContain(remote.Slug, (await PlacementAsync(client, heartbeat)).Eligible);
        Assert.DoesNotContain(heartbeat, await AssignedSlugsAsync(remote.Credential));
    }

    [Theory]
    [InlineData("2", "2")]
    [InlineData("\"3\"", "3")]
    [InlineData("\"50%\"", "50%")]
    public async Task Quorum_round_trips(string written, string expected)
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client, JsonNode.Parse($$"""{ "quorum": {{written}} }"""));

        var workers = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{check}", Ct)).GetProperty("workers");

        Assert.Equal(expected, workers.GetProperty("quorum").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("\"150%\"")]
    [InlineData("\"half\"")]
    [InlineData("true")]
    public async Task Invalid_quorum_is_rejected(string written)
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = Unique("q"),
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            workers = JsonNode.Parse($$"""{ "quorum": {{written}} }"""),
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors").TryGetProperty("workers.quorum", out _));
    }

    [Fact]
    public async Task Assignment_revision_changes_with_checks_and_worker_tags()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var region = Unique("r");
        var worker = await CreateEnrolledWorkerAsync(client, region);
        var check = await CreateCheckAsync(client, new { match = new Dictionary<string, string> { ["vigia:region"] = region } });

        var first = await RevisionAsync(worker.Credential);
        Assert.Equal(first, await RevisionAsync(worker.Credential));

        (await client.PutAsJsonAsync($"/api/v1/checks/{check}", new { config = new { url = "http://127.0.0.1:1/x" }, workers = new { match = new Dictionary<string, string> { ["vigia:region"] = region } } }, Ct)).EnsureSuccessStatusCode();
        var afterCheck = await RevisionAsync(worker.Credential);
        (await client.PutAsJsonAsync($"/api/v1/workers/{worker.Slug}", new { region, tags = new { network = "dmz" } }, Ct)).EnsureSuccessStatusCode();
        var afterRetag = await RevisionAsync(worker.Credential);

        Assert.NotEqual(first, afterCheck);
        Assert.NotEqual(afterCheck, afterRetag);
    }

    [Fact]
    public async Task Built_in_worker_only_runs_checks_that_match_it()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var forBuiltIn = await CreateCheckAsync(client, new { match = new Dictionary<string, object?> { ["vigia:builtin"] = null } });
        var elsewhere = await CreateCheckAsync(client, new { match = new { region = Unique("nowhere") } });

        var assigned = (await factory.Services.GetRequiredService<IAssignmentSource>().GetAsync(Ct)).Select(a => a.Slug).ToList();

        Assert.Contains(forBuiltIn, assigned);
        Assert.DoesNotContain(elsewhere, assigned);
    }

    private static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..16];
    }

    private static async Task<string> CreateCheckAsync(HttpClient client, object? workers)
    {
        var slug = Unique("pl");
        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            workers,
        }, Ct);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(Ct));
        return slug;
    }

    private static async Task<(string Slug, string Token)> CreateWorkerAsync(HttpClient client, string region, Dictionary<string, string?>? tags = null)
    {
        var slug = Unique("wk");
        var response = await client.PostAsJsonAsync("/api/v1/workers", new { slug, region, tags }, Ct);
        response.EnsureSuccessStatusCode();
        return (slug, (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("enrollmentToken").GetString()!);
    }

    private async Task<(string Slug, string Credential)> CreateEnrolledWorkerAsync(HttpClient client, string region, Dictionary<string, string?>? tags = null)
    {
        var (slug, token) = await CreateWorkerAsync(client, region, tags);
        return (slug, await EnrollAsync(token));
    }

    private async Task<string> EnrollAsync(string token)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/worker/v1/enroll", new { token }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("credential").GetString()!;
    }

    private async Task<JsonElement> AssignmentsAsync(string credential)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Worker", credential);
        return await client.GetFromJsonAsync<JsonElement>("/worker/v1/assignments", Ct);
    }

    private async Task<List<string>> AssignedSlugsAsync(string credential)
    {
        return [.. (await AssignmentsAsync(credential)).GetProperty("checks").EnumerateArray().Select(c => c.GetProperty("slug").GetString()!)];
    }

    private async Task<string> RevisionAsync(string credential)
    {
        return (await AssignmentsAsync(credential)).GetProperty("revision").GetString()!;
    }

    private static async Task<(string State, List<string> Eligible, List<string> Online)> PlacementAsync(HttpClient client, string check)
    {
        var placement = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{check}", Ct)).GetProperty("placement");
        return (
            placement.GetProperty("state").GetString()!,
            [.. placement.GetProperty("eligible").EnumerateArray().Select(e => e.GetString()!)],
            [.. placement.GetProperty("online").EnumerateArray().Select(e => e.GetString()!)]);
    }
}
