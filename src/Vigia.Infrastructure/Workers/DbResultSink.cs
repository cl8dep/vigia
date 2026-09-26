using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Vigia.Application.Workers;
using Vigia.Application.Results;
using Vigia.Infrastructure.Persistence;
using Vigia.Infrastructure.Persistence.Configurations;

namespace Vigia.Infrastructure.Workers;

/// <summary>
/// Sends results from the built-in worker and webhooks through <see cref="ResultIngestor"/>, one transaction per
/// result, serialized per check.
/// </summary>
public sealed class DbResultSink(IServiceScopeFactory scopes) : IResultSink
{
    /// <summary>Attempts before giving up on a result that keeps losing races.</summary>
    public const int MaxAttempts = 5;

    /// <inheritdoc />
    public async Task WriteAsync(ProbeRecord record, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await IngestAsync(record, ct);
                return;
            }
            catch (DbUpdateException ex) when (attempt < MaxAttempts && IsRace(ex))
            {
                // Another writer (outside this per-check lock) changed the alerts first; the transaction rolled back
                // entirely, so re-ingesting with fresh state is safe.
            }
        }
    }

    private static bool IsRace(DbUpdateException ex)
    {
        return ex is DbUpdateConcurrencyException
            || ex.InnerException is PostgresException { ConstraintName: AlertConfiguration.OneFiringPerRuleAndCheck };
    }

    private async Task IngestAsync(ProbeRecord record, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // One transaction per result, serialized per check with an advisory lock: results for the same check are
        // evaluated one after the other, results for different checks in parallel.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtextextended({0}::text, 0))", [record.CheckId], ct);
        await scope.ServiceProvider.GetRequiredService<ResultIngestor>().IngestAsync(record, ct);
        await transaction.CommitAsync(ct);
    }
}
