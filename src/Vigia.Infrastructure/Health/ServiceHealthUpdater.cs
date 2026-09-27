using Microsoft.EntityFrameworkCore;
using Vigia.Application.Health;
using Vigia.Domain.Alerts;
using Vigia.Domain.Health;
using Vigia.Domain.Rules;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Health;

/// <summary>
/// <see cref="IServiceHealthUpdater"/> on Postgres. A transaction-scoped advisory lock serializes recomputations, so
/// concurrent ingestions never interleave snapshot writes or record a transition twice.
/// </summary>
public sealed class ServiceHealthUpdater(AppDbContext db, TimeProvider time) : IServiceHealthUpdater
{
    /// <inheritdoc />
    public async Task RecomputeAsync(CancellationToken ct)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext('vigia:service-health'))", ct);

        var services = await db.Services.AsNoTracking().Include(s => s.Dependencies).ToListAsync(ct);
        var checks = await db.Checks.AsNoTracking().ToListAsync(ct);
        var firing = (await db.Alerts.AsNoTracking()
                .Where(a => a.State == AlertState.Firing)
                .Select(a => new { a.CheckId, a.Severity })
                .ToListAsync(ct))
            .GroupBy(a => a.CheckId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Severity>)g.Select(a => a.Severity).ToList());

        var results = HealthCalculator.Compute(services, checks, firing);
        var snapshots = await db.ServiceHealth.ToDictionaryAsync(h => h.ServiceId, ct);
        var now = time.GetUtcNow();

        foreach (var (serviceId, result) in results)
        {
            if (!snapshots.TryGetValue(serviceId, out var snapshot))
            {
                db.ServiceHealth.Add(new ServiceHealth(serviceId, result.State, result.Reason, result.Partitions, now));
                db.ServiceHealthChanges.Add(new ServiceHealthChange(serviceId, null, result.State, result.Reason, now));
                continue;
            }

            var previous = snapshot.State;
            if (snapshot.Update(result.State, result.Reason, result.Partitions, now))
            {
                db.ServiceHealthChanges.Add(new ServiceHealthChange(serviceId, previous, result.State, result.Reason, now));
            }
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }
    }
}
