using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Workers;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Rules turning results into alerts, through the same ingestion path workers and webhooks use.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RulesAndAlertsTests(VigiaApiFactory factory)
{
    private readonly Lock _clockLock = new();
    private DateTimeOffset _clock = DateTimeOffset.UtcNow.AddHours(-1);

    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Down_rule_fires_after_for_and_resolves_after_recover()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new { check, when = new { outcome = "down" }, @for = 3, recoverAfter = 2, severity = "critical" });

        await PushAsync(check, Outcome.Down, "timeout");
        await PushAsync(check, Outcome.Down, "timeout");
        Assert.Empty(await AlertsAsync(client, check));

        await PushAsync(check, Outcome.Down, "timeout");
        var fired = Assert.Single(await AlertsAsync(client, check));
        Assert.Equal("firing", fired.GetProperty("state").GetString());
        Assert.Equal("critical", fired.GetProperty("severity").GetString());
        Assert.Equal("timeout", fired.GetProperty("message").GetString());

        await PushAsync(check, Outcome.Up);
        Assert.Equal("firing", Assert.Single(await AlertsAsync(client, check)).GetProperty("state").GetString());

        await PushAsync(check, Outcome.Up);
        var resolved = Assert.Single(await AlertsAsync(client, check));
        Assert.Equal("resolved", resolved.GetProperty("state").GetString());
        Assert.NotEqual(JsonValueKind.Null, resolved.GetProperty("resolvedAt").ValueKind);
    }

    [Fact]
    public async Task A_new_message_updates_the_same_alert()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new { check, when = new { outcome = "down" } });

        await PushAsync(check, Outcome.Down, "timeout");
        await PushAsync(check, Outcome.Down, "connection refused");

        var alert = Assert.Single(await AlertsAsync(client, check));
        Assert.Equal("connection refused", alert.GetProperty("message").GetString());
        Assert.Equal(2, alert.GetProperty("occurrences").GetInt64());
    }

    [Fact]
    public async Task Errors_neither_fire_nor_recover()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new { check, when = new { outcome = "down" }, @for = 2, recoverAfter = 2 });

        await PushAsync(check, Outcome.Down);
        await PushAsync(check, Outcome.Error);
        Assert.Empty(await AlertsAsync(client, check));
        await PushAsync(check, Outcome.Down);
        Assert.Equal("firing", Assert.Single(await AlertsAsync(client, check)).GetProperty("state").GetString());

        await PushAsync(check, Outcome.Up);
        await PushAsync(check, Outcome.Error);
        Assert.Equal("firing", Assert.Single(await AlertsAsync(client, check)).GetProperty("state").GetString());
        await PushAsync(check, Outcome.Up);
        Assert.Equal("resolved", Assert.Single(await AlertsAsync(client, check)).GetProperty("state").GetString());
    }

    [Fact]
    public async Task Selector_rule_with_threshold_applies_only_to_matching_checks()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var service = $"svc-{Guid.NewGuid():N}"[..12];
        var matching = await CreateCheckAsync(client, new Dictionary<string, string?> { ["service"] = service });
        var other = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new
        {
            selector = new Dictionary<string, string> { ["service"] = service, ["vigia:plugin"] = "vigia.check.http" },
            when = new { dimension = "latency", above = 800 },
            @for = 2,
        });

        await PushAsync(matching, Outcome.Up, latency: 900);
        await PushAsync(other, Outcome.Up, latency: 900);
        await PushAsync(matching, Outcome.Up, latency: 950);
        await PushAsync(other, Outcome.Up, latency: 950);

        var alert = Assert.Single(await AlertsAsync(client, matching));
        Assert.Equal("latency is 950ms, above 800ms.", alert.GetProperty("message").GetString());
        Assert.Empty(await AlertsAsync(client, other));

        await PushAsync(matching, Outcome.Up, latency: 100);
        Assert.Equal("resolved", Assert.Single(await AlertsAsync(client, matching)).GetProperty("state").GetString());
    }

    [Fact]
    public async Task Rules_are_additive_and_refiring_opens_a_new_alert()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new { check, when = new { outcome = "down" } });
        await CreateRuleAsync(client, new { check, when = new { dimension = "latency", above = 500 } });

        await PushAsync(check, Outcome.Down, latency: 600);
        Assert.Equal(2, (await AlertsAsync(client, check)).Length);

        await PushAsync(check, Outcome.Up, latency: 10);
        await PushAsync(check, Outcome.Down, latency: 10);
        var alerts = await AlertsAsync(client, check);
        Assert.Equal(3, alerts.Length);
        Assert.Single(alerts, a => a.GetProperty("state").GetString() == "firing");
    }

    [Fact]
    public async Task Invalid_rules_are_rejected_with_the_field()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);

        await AssertInvalidAsync(client, new { check, selector = new { env = "prod" }, when = new { outcome = "down" } }, "check");
        await AssertInvalidAsync(client, new { when = new { outcome = "down" } }, "check");
        await AssertInvalidAsync(client, new { check = "no-such-check", when = new { outcome = "down" } }, "check");
        await AssertInvalidAsync(client, new { check, when = new { outcome = "up" } }, "when");
        await AssertInvalidAsync(client, new { check, when = new { dimension = "latency", above = 1, below = 2 } }, "when");
        await AssertInvalidAsync(client, new { check, when = new { dimension = "days-to-expiry", below = 14 } }, "when.dimension");
        await AssertInvalidAsync(client, new { check, when = new { outcome = "down" }, @for = 0 }, "when");
        await AssertInvalidAsync(client, new { check, when = new { outcome = "down" }, severity = "panic" }, "severity");
    }

    [Fact]
    public async Task Rule_crud_round_trips()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        var slug = await CreateRuleAsync(client, new { check, when = new { outcome = "down" } });

        var duplicate = await client.PostAsJsonAsync("/api/v1/rules", new { slug, check, when = new { outcome = "down" } }, Ct);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var update = await client.PutAsJsonAsync($"/api/v1/rules/{slug}", new { check, when = new { dimension = "latency", above = 300 }, @for = 4, enabled = false }, Ct);
        update.EnsureSuccessStatusCode();
        var rule = await client.GetFromJsonAsync<JsonElement>($"/api/v1/rules/{slug}", Ct);
        Assert.Equal(check, rule.GetProperty("check").GetString());
        Assert.Equal(300, rule.GetProperty("when").GetProperty("above").GetDouble());
        Assert.Equal(4, rule.GetProperty("for").GetInt32());
        Assert.False(rule.GetProperty("enabled").GetBoolean());
        Assert.Contains(await client.GetFromJsonAsync<JsonElement[]>("/api/v1/rules", Ct) ?? [], r => r.GetProperty("slug").GetString() == slug);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/rules/{slug}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/rules/{slug}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_results_open_a_single_alert()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new { check, when = new { outcome = "down" } });

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => PushAsync(check, Outcome.Down)));

        var alert = Assert.Single(await AlertsAsync(client, check));
        Assert.Equal(8, alert.GetProperty("occurrences").GetInt64());
    }

    [Fact]
    public async Task Disabled_rules_are_not_evaluated()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var check = await CreateCheckAsync(client);
        await CreateRuleAsync(client, new { check, when = new { outcome = "down" }, enabled = false });

        await PushAsync(check, Outcome.Down);

        Assert.Empty(await AlertsAsync(client, check));
    }

    private static async Task<string> CreateCheckAsync(HttpClient client, IReadOnlyDictionary<string, string?>? tags = null)
    {
        var slug = $"rules-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            tags,
            workers = Placement.BuiltInOnly,
        }, Ct);
        response.EnsureSuccessStatusCode();
        return slug;
    }

    private static async Task<string> CreateRuleAsync(HttpClient client, object spec)
    {
        var body = JsonSerializer.SerializeToNode(spec, JsonSerializerOptions.Web)!.AsObject();
        var slug = $"rule-{Guid.NewGuid():N}"[..20];
        body["slug"] = slug;
        var response = await client.PostAsJsonAsync("/api/v1/rules", body, Ct);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(Ct));
        return slug;
    }

    private static async Task AssertInvalidAsync(HttpClient client, object spec, string field)
    {
        var body = JsonSerializer.SerializeToNode(spec, JsonSerializerOptions.Web)!.AsObject();
        body["slug"] = $"bad-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/v1/rules", body, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _), $"Expected an error on '{field}', got {errors}.");
    }

    private static async Task<JsonElement[]> AlertsAsync(HttpClient client, string check)
    {
        return (await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/alerts?check={check}", Ct))!;
    }

    private async Task PushAsync(string checkSlug, Outcome outcome, string? message = null, double? latency = null)
    {
        Guid checkId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            checkId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Checks
                .Where(c => c.Slug == checkSlug).Select(c => c.Id).SingleAsync(Ct);
        }

        var at = NextTick();
        var measurements = latency is null ? new Dictionary<string, double>() : new Dictionary<string, double> { ["latency"] = latency.Value };
        // Reported as the built-in worker: a single-node install, independent of which workers other tests left online.
        var record = new ProbeRecord(Guid.CreateVersion7(at), checkId, "builtin", outcome, measurements, message, 1, at);
        await factory.Services.GetRequiredService<IResultSink>().WriteAsync(record, Ct);
    }

    private DateTimeOffset NextTick()
    {
        lock (_clockLock)
        {
            _clock = _clock.AddSeconds(30);
            return _clock;
        }
    }
}
