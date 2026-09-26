using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Domain.Results;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Load;

/// <summary>
/// The built-in agent with many real HTTP checks. Explicit: slow, run on demand.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class BuiltInAgentLoadTests(VigiaApiFactory factory)
{
    private const int CheckCount = 200;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RunFor = TimeSpan.FromSeconds(30);

    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact(Explicit = true)]
    public async Task Many_checks_all_get_probed_on_schedule()
    {
        await using var target = await LocalHttpServer.StartAsync(Ct);
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var prefix = $"load-{Guid.NewGuid():N}"[..10];
        for (var i = 0; i < CheckCount; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/checks", new
            {
                slug = $"{prefix}-{i}",
                plugin = "vigia.check.http",
                config = new { url = $"{target.BaseUrl}/ok" },
                interval = "10s",
            }, Ct);
            response.EnsureSuccessStatusCode();
        }

        var started = DateTimeOffset.UtcNow;
        await using (var withAgent = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Agent:BuiltInEnabled", "true");
            b.UseSetting("Agent:RefreshInterval", "00:00:01");
        }))
        {
            withAgent.CreateClient();
            await Task.Delay(RunFor, Ct);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var results = await db.CheckResults.AsNoTracking()
            .Join(db.Checks.Where(c => c.Slug.StartsWith(prefix)), r => r.CheckId, c => c.Id, (r, c) => new { c.Slug, r.Outcome, r.ObservedAt, r.DurationMs })
            .ToListAsync(Ct);

        var perCheck = results.GroupBy(r => r.Slug).ToDictionary(g => g.Key, g => g.OrderBy(r => r.ObservedAt).ToList());
        var gaps = perCheck.Values.SelectMany(rs => rs.Zip(rs.Skip(1), (a, b) => (b.ObservedAt - a.ObservedAt).TotalSeconds)).ToList();
        var firstOffsets = perCheck.Values.Select(rs => (rs[0].ObservedAt - started).TotalSeconds).OrderBy(x => x).ToList();
        var durations = results.Select(r => r.DurationMs).OrderBy(x => x).ToList();

        TestContext.Current.SendDiagnosticMessage(
            $"checks={CheckCount} probed={perCheck.Count} results={results.Count} down/error={results.Count(r => r.Outcome != ResultOutcome.Up)}\n" +
            $"results per check: min={perCheck.Values.Min(v => v.Count)} max={perCheck.Values.Max(v => v.Count)}\n" +
            $"gap between probes (s): min={gaps.Min():F2} avg={gaps.Average():F2} max={gaps.Max():F2}\n" +
            $"first probe offset (s): min={firstOffsets[0]:F2} p50={firstOffsets[firstOffsets.Count / 2]:F2} max={firstOffsets[^1]:F2}\n" +
            $"probe duration (ms): p50={durations[durations.Count / 2]:F1} p95={durations[(int)(durations.Count * 0.95)]:F1} max={durations[^1]:F1}");

        Assert.Equal(CheckCount, perCheck.Count);
        Assert.All(perCheck.Values, rs => Assert.True(rs.Count >= 2, "Every check should have been probed at least twice."));
        Assert.All(results, r => Assert.Equal(ResultOutcome.Up, r.Outcome));
        Assert.All(gaps, g => Assert.InRange(g, Interval.TotalSeconds - 0.5, Interval.TotalSeconds + 1.5));
    }
}
