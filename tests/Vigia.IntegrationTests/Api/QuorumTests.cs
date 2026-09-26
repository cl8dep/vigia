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
/// Rules across several workers: per-worker streaks, quorum, freshness and where it fails.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class QuorumTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Default_majority_fires_only_when_most_workers_fail()
    {
        var (client, check, workers) = await SetUpAsync(quorum: null);
        var t = DateTimeOffset.UtcNow.AddHours(-1);

        await CycleAsync(check, t, (workers[0], Outcome.Down), (workers[1], Outcome.Up), (workers[2], Outcome.Up));
        Assert.Empty(await AlertsAsync(client, check));

        await CycleAsync(check, t.AddSeconds(30), (workers[0], Outcome.Down), (workers[1], Outcome.Down), (workers[2], Outcome.Up));
        var alert = Assert.Single(await AlertsAsync(client, check));
        var message = alert.GetProperty("message").GetString()!;
        Assert.Contains("2 of 3", message);
        Assert.Contains(workers[0].Region, message);
        Assert.Contains(workers[1].Region, message);
        Assert.DoesNotContain(workers[2].Region, message);
    }

    [Fact]
    public async Task Quorum_of_one_fires_on_any_worker()
    {
        var (client, check, workers) = await SetUpAsync(quorum: 1);

        await CycleAsync(check, DateTimeOffset.UtcNow.AddHours(-1), (workers[0], Outcome.Down), (workers[1], Outcome.Up), (workers[2], Outcome.Up));

        Assert.Contains("1 of 3", Assert.Single(await AlertsAsync(client, check)).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Hundred_percent_needs_every_worker()
    {
        var (client, check, workers) = await SetUpAsync(quorum: "100%");
        var t = DateTimeOffset.UtcNow.AddHours(-1);

        await CycleAsync(check, t, (workers[0], Outcome.Down), (workers[1], Outcome.Down), (workers[2], Outcome.Up));
        Assert.Empty(await AlertsAsync(client, check));

        await CycleAsync(check, t.AddSeconds(30), (workers[0], Outcome.Down), (workers[1], Outcome.Down), (workers[2], Outcome.Down));
        Assert.Contains("3 of 3", Assert.Single(await AlertsAsync(client, check)).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Offline_workers_without_fresh_results_do_not_count()
    {
        var (client, check, workers) = await SetUpAsync(quorum: null, online: 2);
        var t = DateTimeOffset.UtcNow.AddHours(-1);

        // The third worker last reported ten minutes before; with a one minute interval it is stale.
        await CycleAsync(check, t.AddMinutes(-10), (workers[2], Outcome.Up));
        await CycleAsync(check, t, (workers[0], Outcome.Down), (workers[1], Outcome.Up));
        Assert.Empty(await AlertsAsync(client, check));

        await CycleAsync(check, t.AddSeconds(30), (workers[0], Outcome.Down), (workers[1], Outcome.Down));
        Assert.Contains("2 of 2", Assert.Single(await AlertsAsync(client, check)).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Resolves_when_fewer_than_quorum_are_still_failing()
    {
        var (client, check, workers) = await SetUpAsync(quorum: null);
        var t = DateTimeOffset.UtcNow.AddHours(-1);

        await CycleAsync(check, t, (workers[0], Outcome.Down), (workers[1], Outcome.Down), (workers[2], Outcome.Up));
        Assert.Equal("firing", Assert.Single(await AlertsAsync(client, check)).GetProperty("state").GetString());

        await CycleAsync(check, t.AddSeconds(30), (workers[0], Outcome.Up));

        Assert.Equal("resolved", Assert.Single(await AlertsAsync(client, check)).GetProperty("state").GetString());
    }

    private async Task<(HttpClient Client, string Check, (string Slug, string Region)[] Workers)> SetUpAsync(object? quorum, int online = 3)
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var tag = $"q-{Guid.NewGuid():N}"[..12];
        var workers = new (string Slug, string Region)[3];
        for (var i = 0; i < workers.Length; i++)
        {
            var slug = $"qw-{Guid.NewGuid():N}"[..16];
            var region = $"reg{i}-{Guid.NewGuid():N}"[..12];
            var registered = await client.PostAsJsonAsync("/api/v1/workers", new { slug, region, tags = new { quorum = tag } }, Ct);
            registered.EnsureSuccessStatusCode();
            workers[i] = (slug, region);

            // Enrolling reports the worker, so it is online like a real running worker.
            if (i < online)
            {
                var token = (await registered.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("enrollmentToken").GetString();
                (await factory.CreateClient().PostAsJsonAsync("/worker/v1/enroll", new { token }, Ct)).EnsureSuccessStatusCode();
            }
        }

        var check = $"qc-{Guid.NewGuid():N}"[..16];
        var created = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = check,
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
            interval = "1m",
            workers = new { match = new { quorum = tag }, quorum },
        }, Ct);
        created.EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/rules", new { slug = $"qr-{Guid.NewGuid():N}"[..16], check, when = new { outcome = "down" } }, Ct)).EnsureSuccessStatusCode();
        return (client, check, workers);
    }

    private async Task CycleAsync(string checkSlug, DateTimeOffset at, params ((string Slug, string Region) Worker, Outcome Outcome)[] results)
    {
        Guid checkId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            checkId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Checks.Where(c => c.Slug == checkSlug).Select(c => c.Id).SingleAsync(Ct);
        }

        var sink = factory.Services.GetRequiredService<IResultSink>();
        for (var i = 0; i < results.Length; i++)
        {
            var observed = at.AddSeconds(i);
            await sink.WriteAsync(
                new ProbeRecord(Guid.CreateVersion7(observed), checkId, results[i].Worker.Slug, results[i].Outcome, new Dictionary<string, double>(), "down", 1, observed),
                Ct);
        }
    }

    private static async Task<JsonElement[]> AlertsAsync(HttpClient client, string check)
    {
        return (await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/alerts?check={check}", Ct))!;
    }
}
