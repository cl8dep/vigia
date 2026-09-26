using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Domain.Checks;
using Vigia.Domain.Common;
using Vigia.Domain.Results;
using Vigia.Infrastructure.Persistence;
using Vigia.Infrastructure.Results;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Maintenance;

/// <summary>
/// Hourly rollups and retention against a real Postgres.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ResultMaintenanceTests(VigiaApiFactory factory)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset CurrentHour = new(Now.Year, Now.Month, Now.Day, Now.Hour, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Hour = CurrentHour.AddHours(-3);

    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Rolls_up_complete_hours_per_agent_with_dimension_stats()
    {
        var checkId = await CreateCheckAsync();
        await AddResultsAsync(
            Result(checkId, "builtin", ResultOutcome.Up, Hour.AddMinutes(1), latency: 100),
            Result(checkId, "builtin", ResultOutcome.Up, Hour.AddMinutes(2), latency: 200),
            Result(checkId, "builtin", ResultOutcome.Down, Hour.AddMinutes(3), latency: 300),
            Result(checkId, "builtin", ResultOutcome.Error, Hour.AddMinutes(4)),
            Result(checkId, "eu", ResultOutcome.Up, Hour.AddMinutes(5), latency: 50),
            Result(checkId, "builtin", ResultOutcome.Up, Now, latency: 10));

        await RunMaintenanceAsync();

        var rollups = await RollupsAsync(checkId);
        Assert.Equal(2, rollups.Count);
        Assert.All(rollups, r => Assert.Equal(Hour, r.HourStart));

        var builtin = rollups.Single(r => r.Agent == "builtin");
        Assert.Equal((2, 1, 1), (builtin.Up, builtin.Down, builtin.Error));
        var latency = builtin.Dimensions["latency"];
        Assert.Equal(100, latency.Min);
        Assert.Equal(200, latency.Avg);
        Assert.Equal(300, latency.Max);
        Assert.Equal(290, latency.P95, precision: 6);
        Assert.Equal(3, latency.Count);

        Assert.Equal(1, rollups.Single(r => r.Agent == "eu").Up);
    }

    [Fact]
    public async Task Late_results_are_folded_into_their_hour()
    {
        var checkId = await CreateCheckAsync();
        await AddResultsAsync(Result(checkId, "builtin", ResultOutcome.Up, Hour.AddMinutes(1), latency: 100));
        await RunMaintenanceAsync();

        await AddResultsAsync(Result(checkId, "builtin", ResultOutcome.Down, Hour.AddMinutes(30), latency: 900));
        await RunMaintenanceAsync();

        var rollup = Assert.Single(await RollupsAsync(checkId));
        Assert.Equal((1, 1), (rollup.Up, rollup.Down));
        Assert.Equal(900, rollup.Dimensions["latency"].Max);
    }

    [Fact]
    public async Task Deletes_data_past_retention()
    {
        var checkId = await CreateCheckAsync();
        var expired = Result(checkId, "builtin", ResultOutcome.Up, Now.AddDays(-15), latency: 1);
        var kept = Result(checkId, "builtin", ResultOutcome.Up, Now.AddDays(-13), latency: 1);
        await AddResultsAsync(expired, kept);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO check_result_rollups (check_id, agent, hour_start, up, down, error, dimensions) VALUES ({0}, 'builtin', {1}, 1, 0, 0, '{{}}')",
                [checkId, CurrentHour.AddDays(-401)], Ct);
        }

        await RunMaintenanceAsync();

        await using var verify = factory.Services.CreateAsyncScope();
        var context = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var remaining = await context.CheckResults.Where(r => r.CheckId == checkId).Select(r => r.Id).ToListAsync(Ct);
        Assert.Equal([kept.Id], remaining);
        Assert.DoesNotContain(await RollupsAsync(checkId), r => r.HourStart < Now.AddDays(-400));
    }

    private static CheckResult Result(Guid checkId, string agent, ResultOutcome outcome, DateTimeOffset at, double? latency = null)
    {
        var measurements = latency is null ? new Dictionary<string, double>() : new Dictionary<string, double> { ["latency"] = latency.Value };
        return new CheckResult(Guid.CreateVersion7(at), checkId, agent, outcome, measurements, null, 1, at);
    }

    private async Task<Guid> CreateCheckAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var check = new Check($"rollup-{Guid.NewGuid():N}"[..20], "rollup", "vigia.check.http", "1.0.0", """{"url":"http://127.0.0.1:1/"}""", TimeSpan.FromMinutes(1), ManagedBy.Ui);
        db.Checks.Add(check);
        await db.SaveChangesAsync(Ct);
        return check.Id;
    }

    private async Task AddResultsAsync(params CheckResult[] results)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckResults.AddRange(results);
        await db.SaveChangesAsync(Ct);
    }

    private async Task RunMaintenanceAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ResultMaintenance>().RunOnceAsync(Now, Ct);
    }

    private async Task<List<CheckResultRollup>> RollupsAsync(Guid checkId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CheckResultRollups.AsNoTracking().Where(r => r.CheckId == checkId).ToListAsync(Ct);
    }
}
