using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// The built-in agent probes checks on its own and stores results in Postgres.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class BuiltInAgentTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Stores_results_without_manual_probes()
    {
        await using var target = await LocalHttpServer.StartAsync(Ct);
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = $"agent-{Guid.NewGuid():N}"[..20];
        var create = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url = $"{target.BaseUrl}/ok" },
            interval = "10s",
        }, Ct);
        create.EnsureSuccessStatusCode();

        await using var withAgent = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Agent:BuiltInEnabled", "true");
            b.UseSetting("Agent:RefreshInterval", "00:00:01");
            b.UseSetting("Agent:InitialJitter", "00:00:00");
        });
        withAgent.CreateClient();

        JsonElement[] results = [];
        await Eventually.TrueAsync(() =>
        {
            results = client.GetFromJsonAsync<JsonElement[]>($"/api/v1/checks/{slug}/results", Ct).GetAwaiter().GetResult()!;
            return results.Length > 0;
        }, TimeSpan.FromSeconds(10));

        var latest = results[0];
        Assert.Equal("up", latest.GetProperty("outcome").GetString());
        Assert.Equal("builtin", latest.GetProperty("agent").GetString());
        Assert.True(latest.GetProperty("measurements").TryGetProperty("latency", out _));
    }

    [Fact]
    public async Task Results_endpoint_validates_input()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var unknown = await client.GetAsync("/api/v1/checks/does-not-exist/results", Ct);
        var badLimit = await client.GetAsync("/api/v1/checks/does-not-exist/results?limit=0", Ct);

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badLimit.StatusCode);
    }
}
