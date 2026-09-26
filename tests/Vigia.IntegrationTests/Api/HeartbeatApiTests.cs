using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Agents;
using Vigia.Domain.Checks;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Heartbeat checks end to end: token issuance, the public ping webhook, receipts and evaluation.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class HeartbeatApiTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Create_returns_the_token_once()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token) = await CreateAsync(client);

        var check = await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(JsonValueKind.Null, check.GetProperty("webhookToken").ValueKind);
        Assert.Equal(["ping"], check.GetProperty("webhooks").EnumerateArray().Select(w => w.GetString()));
    }

    [Fact]
    public async Task Ping_with_token_is_accepted_and_the_check_is_up()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token) = await CreateAsync(client);
        var anonymous = factory.CreateClient();

        var byQuery = await anonymous.GetAsync($"/api/v1/hooks/{slug}/ping?token={token}", Ct);
        var byHeader = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/hooks/{slug}/ping");
        byHeader.Headers.Add("X-Vigia-Token", token);
        var headerResponse = await anonymous.SendAsync(byHeader, Ct);

        Assert.Equal(HttpStatusCode.NoContent, byQuery.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, headerResponse.StatusCode);

        var results = await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/checks/{slug}/results", Ct);
        Assert.Contains(results!, r => r.GetProperty("agent").GetString() == "webhook" && r.GetProperty("outcome").GetString() == "up");
        Assert.Equal("up", (await ProbeAsync(client, slug)).GetProperty("outcome").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checkId = await db.Checks.Where(c => c.Slug == slug).Select(c => c.Id).SingleAsync(Ct);
        var receipt = await db.WebhookReceipts.SingleAsync(r => r.CheckId == checkId && r.Webhook == "ping", Ct);
        Assert.Equal(2, receipt.Count);
    }

    [Fact]
    public async Task Wrong_or_missing_token_is_unauthorized()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _) = await CreateAsync(client);
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/hooks/{slug}/ping?token=wrong", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/hooks/{slug}/ping", Ct)).StatusCode);
    }

    [Fact]
    public async Task Undeclared_webhooks_do_not_exist()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token) = await CreateAsync(client);
        var http = $"http-{Guid.NewGuid():N}"[..20];
        (await client.PostAsJsonAsync("/api/v1/checks", new { slug = http, plugin = "vigia.check.http", config = new { url = "http://127.0.0.1:1/" } }, Ct)).EnsureSuccessStatusCode();
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/hooks/{slug}/other?token={token}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/hooks/{http}/ping?token={token}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/v1/hooks/no-such-check/ping?token={token}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Waiting_for_the_first_ping_is_an_error_not_down()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _) = await CreateAsync(client);

        var waiting = await ProbeAsync(client, slug);

        Assert.Equal("error", waiting.GetProperty("outcome").GetString());
        Assert.Contains("Waiting for the first ping", waiting.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Never_pinged_past_the_first_period_is_down()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _) = await CreateAsync(client);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ExecuteSqlRawAsync(
                "UPDATE checks SET created_at = {0} WHERE slug = {1}", [DateTimeOffset.UtcNow.AddMinutes(-10), slug], Ct);
        }

        var result = await ProbeAsync(client, slug);

        Assert.Equal("down", result.GetProperty("outcome").GetString());
        Assert.Contains("No ping received since the check was created", result.GetProperty("message").GetString());
        Assert.InRange(result.GetProperty("measurements").GetProperty("since-last-ping").GetDouble(), 590, 700);
    }

    [Fact]
    public async Task A_job_that_never_runs_opens_an_alert()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _) = await CreateAsync(client);
        var rule = await client.PostAsJsonAsync("/api/v1/rules", new { slug = $"r-{Guid.NewGuid():N}"[..20], check = slug, when = new { outcome = "down" } }, Ct);
        rule.EnsureSuccessStatusCode();

        Check check;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("UPDATE checks SET created_at = {0} WHERE slug = {1}", [DateTimeOffset.UtcNow.AddMinutes(-10), slug], Ct);
            check = await db.Checks.AsNoTracking().SingleAsync(c => c.Slug == slug, Ct);
        }

        // Same path as the built-in agent: probe, then ingest.
        var record = await factory.Services.GetRequiredService<IProbeExecutor>().ExecuteAsync(CheckAssignment.From(check), Ct);
        await factory.Services.GetRequiredService<IResultSink>().WriteAsync(record, Ct);

        var alert = Assert.Single(await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/alerts?check={slug}", Ct) ?? []);
        Assert.Equal("firing", alert.GetProperty("state").GetString());
        Assert.Contains("No ping received since the check was created", alert.GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_stale_ping_is_down()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _) = await CreateAsync(client);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var checkId = await db.Checks.Where(c => c.Slug == slug).Select(c => c.Id).SingleAsync(Ct);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO webhook_receipts (check_id, webhook, last_received_at, count) VALUES ({0}, 'ping', {1}, 1)",
                [checkId, DateTimeOffset.UtcNow.AddMinutes(-10)], Ct);
        }

        var stale = await ProbeAsync(client, slug);
        Assert.Equal("down", stale.GetProperty("outcome").GetString());
        Assert.InRange(stale.GetProperty("measurements").GetProperty("since-last-ping").GetDouble(), 590, 700);
    }

    [Fact]
    public async Task Rotating_the_token_invalidates_the_old_one()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, oldToken) = await CreateAsync(client);

        var rotated = await client.PostAsync($"/api/v1/checks/{slug}/webhook-token", null, Ct);
        rotated.EnsureSuccessStatusCode();
        var newToken = (await rotated.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("token").GetString();
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v1/hooks/{slug}/ping?token={oldToken}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.GetAsync($"/api/v1/hooks/{slug}/ping?token={newToken}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Disabled_check_rejects_pings()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token) = await CreateAsync(client);
        (await client.PutAsJsonAsync($"/api/v1/checks/{slug}", new { config = new { }, enabled = false }, Ct)).EnsureSuccessStatusCode();

        var response = await factory.CreateClient().GetAsync($"/api/v1/hooks/{slug}/ping?token={token}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<(string Slug, string Token)> CreateAsync(HttpClient client)
    {
        var slug = $"beat-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.heartbeat",
            config = new { every = "5m", grace = "1m" },
        }, Ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        return (slug, body.GetProperty("webhookToken").GetString()!);
    }

    private static async Task<JsonElement> ProbeAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsync($"/api/v1/checks/{slug}/probe", null, Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}
