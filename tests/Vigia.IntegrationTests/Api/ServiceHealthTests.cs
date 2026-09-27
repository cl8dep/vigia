using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Health;
using Vigia.Application.Workers;
using Vigia.Domain.Alerts;
using Vigia.Domain.Rules;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Service health derived from alerts, partitions and dependencies, stored with its history.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ServiceHealthTests(VigiaApiFactory factory)
{
    private DateTimeOffset _clock = DateTimeOffset.UtcNow.AddHours(-1);

    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Unknown_without_checks_and_operational_with_healthy_ones()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var empty = await CreateServiceAsync(client, new { });
        var tag = Unique("s");
        await CreateCheckAsync(client, new() { ["service"] = tag });
        var healthy = await CreateServiceAsync(client, new { checks = new { service = tag } });

        Assert.Equal("unknown", (await HealthAsync(client, empty)).State);
        Assert.Equal("operational", (await HealthAsync(client, healthy)).State);
    }

    [Fact]
    public async Task Critical_alert_takes_the_service_down_and_back_with_history()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("s");
        var check = await CreateCheckAsync(client, new() { ["service"] = tag });
        await CreateRuleAsync(client, check, "critical");
        var service = await CreateServiceAsync(client, new { checks = new { service = tag } });

        await PushAsync(check, Outcome.Down);
        var down = await HealthAsync(client, service);
        await PushAsync(check, Outcome.Up);
        var back = await HealthAsync(client, service);

        Assert.Equal("down", down.State);
        Assert.Contains(check, down.Reason);
        Assert.Equal("operational", back.State);
        var changes = await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/services/{service}/health-changes", Ct);
        Assert.Equal(
            ["operational", "down", "operational"],
            changes!.Reverse().Select(c => c.GetProperty("to").GetString()));
        Assert.Equal(JsonValueKind.Null, changes!.Last().GetProperty("from").ValueKind);
    }

    [Fact]
    public async Task Warnings_degrade_and_info_is_ignored()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("s");
        var warned = await CreateCheckAsync(client, new() { ["service"] = tag });
        var informed = await CreateCheckAsync(client, new() { ["service"] = tag });
        await CreateRuleAsync(client, warned, "warning");
        await CreateRuleAsync(client, informed, "info");
        var service = await CreateServiceAsync(client, new { checks = new { service = tag } });

        await PushAsync(informed, Outcome.Down);
        Assert.Equal("operational", (await HealthAsync(client, service)).State);

        await PushAsync(warned, Outcome.Down);
        Assert.Equal("degraded", (await HealthAsync(client, service)).State);
    }

    [Fact]
    public async Task One_partition_down_is_a_partial_outage()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("s");
        var eu = await CreateCheckAsync(client, new() { ["service"] = tag, ["region"] = "eu-west" });
        var us = await CreateCheckAsync(client, new() { ["service"] = tag, ["region"] = "us-east" });
        await CreateRuleAsync(client, eu, "critical");
        await CreateRuleAsync(client, us, "critical");
        var service = await CreateServiceAsync(client, new { checks = new { service = tag }, partitionBy = "region" });

        await PushAsync(eu, Outcome.Down);
        var partial = await HealthAsync(client, service);
        await PushAsync(us, Outcome.Down);
        var total = await HealthAsync(client, service);

        Assert.Equal("partial-outage", partial.State);
        Assert.Equal("eu-west down", partial.Reason);
        Assert.Equal("down", partial.Partitions["eu-west"]);
        Assert.Equal("operational", partial.Partitions["us-east"]);
        Assert.Equal("down", total.State);
    }

    [Fact]
    public async Task Dependencies_propagate_by_mode()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("s");
        var check = await CreateCheckAsync(client, new() { ["service"] = tag });
        await CreateRuleAsync(client, check, "critical");
        var sabre = await CreateServiceAsync(client, new { checks = new { service = tag }, tags = new Dictionary<string, string?> { ["vigia:external"] = null } });
        var booking = await CreateServiceAsync(client, new { dependsOn = new[] { new { service = sabre, mode = "blocking" } } });
        Assert.Equal("operational", (await HealthAsync(client, booking)).State);
        var search = await CreateServiceAsync(client, new { dependsOn = new[] { new { service = sabre, mode = "soft" } } });
        var docs = await CreateServiceAsync(client, new { dependsOn = new[] { new { service = sabre, mode = "advisory" } } });

        await PushAsync(check, Outcome.Down);

        var bookingHealth = await HealthAsync(client, booking);
        Assert.Equal("down", bookingHealth.State);
        Assert.Contains($"depends on {sabre} (down, blocking)", bookingHealth.Reason);
        Assert.Equal("degraded", (await HealthAsync(client, search)).State);
        Assert.Equal("unknown", (await HealthAsync(client, docs)).State);
    }

    [Fact]
    public async Task Retagging_a_check_moves_it_out_of_the_service()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("s");
        var check = await CreateCheckAsync(client, new() { ["service"] = tag });
        await CreateRuleAsync(client, check, "critical");
        var service = await CreateServiceAsync(client, new { checks = new { service = tag } });
        await PushAsync(check, Outcome.Down);
        Assert.Equal("down", (await HealthAsync(client, service)).State);

        (await client.PutAsJsonAsync($"/api/v1/checks/{check}", new { config = new { url = "http://127.0.0.1:1/" }, tags = new { service = Unique("elsewhere") } }, Ct)).EnsureSuccessStatusCode();

        Assert.Equal("unknown", (await HealthAsync(client, service)).State);
    }

    [Fact]
    public async Task Stored_health_always_equals_a_fresh_recomputation()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = Unique("s");
        var check = await CreateCheckAsync(client, new() { ["service"] = tag, ["region"] = "eu-west" });
        await CreateRuleAsync(client, check, "critical");
        var service = await CreateServiceAsync(client, new { checks = new { service = tag }, partitionBy = "region" });
        await CreateServiceAsync(client, new { dependsOn = new[] { new { service, mode = "soft" } } });
        await PushAsync(check, Outcome.Down);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var changesBefore = await db.ServiceHealthChanges.CountAsync(Ct);
        await scope.ServiceProvider.GetRequiredService<IServiceHealthUpdater>().RecomputeAsync(Ct);

        var services = await db.Services.AsNoTracking().Include(s => s.Dependencies).ToListAsync(Ct);
        var checks = await db.Checks.AsNoTracking().ToListAsync(Ct);
        var firing = (await db.Alerts.AsNoTracking().Where(a => a.State == AlertState.Firing).ToListAsync(Ct))
            .GroupBy(a => a.CheckId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Severity>)g.Select(a => a.Severity).ToList());
        var fresh = HealthCalculator.Compute(services, checks, firing);
        var stored = await db.ServiceHealth.AsNoTracking().ToDictionaryAsync(h => h.ServiceId, Ct);

        Assert.All(fresh, f => Assert.Equal(f.Value.State, stored[f.Key].State));
        Assert.Equal(changesBefore, await db.ServiceHealthChanges.CountAsync(Ct));
    }

    private static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..16];
    }

    private static async Task<string> CreateCheckAsync(HttpClient client, Dictionary<string, string?> tags)
    {
        var slug = Unique("hc");
        (await client.PostAsJsonAsync("/api/v1/checks", new { slug, plugin = "vigia.check.http", config = new { url = "http://127.0.0.1:1/" }, tags, workers = Placement.BuiltInOnly }, Ct)).EnsureSuccessStatusCode();
        return slug;
    }

    private static async Task CreateRuleAsync(HttpClient client, string check, string severity)
    {
        (await client.PostAsJsonAsync("/api/v1/rules", new { slug = Unique("hr"), check, when = new { outcome = "down" }, severity }, Ct)).EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateServiceAsync(HttpClient client, object spec)
    {
        var body = JsonSerializer.SerializeToNode(spec, JsonSerializerOptions.Web)!.AsObject();
        var slug = Unique("hs");
        body["slug"] = slug;
        var response = await client.PostAsJsonAsync("/api/v1/services", body, Ct);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(Ct));
        return slug;
    }

    private static async Task<(string State, string Reason, Dictionary<string, string> Partitions)> HealthAsync(HttpClient client, string service)
    {
        var health = (await client.GetFromJsonAsync<JsonElement>($"/api/v1/services/{service}", Ct)).GetProperty("health");
        return (
            health.GetProperty("state").GetString()!,
            health.GetProperty("reason").GetString()!,
            health.GetProperty("partitions").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!));
    }

    private async Task PushAsync(string checkSlug, Outcome outcome)
    {
        Guid checkId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            checkId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Checks.Where(c => c.Slug == checkSlug).Select(c => c.Id).SingleAsync(Ct);
        }

        _clock = _clock.AddSeconds(30);
        var record = new ProbeRecord(Guid.CreateVersion7(_clock), checkId, "builtin", outcome, new Dictionary<string, double>(), "connection refused", 1, _clock);
        await factory.Services.GetRequiredService<IResultSink>().WriteAsync(record, Ct);
    }
}
