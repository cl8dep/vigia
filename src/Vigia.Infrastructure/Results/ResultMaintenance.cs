using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Results;

/// <summary>
/// Rolls raw results up into hourly summaries and deletes data past retention. Postgres-specific SQL.
/// </summary>
/// <remarks>
/// Only complete hours are rolled up. The window starts <see cref="RetentionOptions.RollupLookback"/> before the
/// latest rollup (or at the oldest result on the first run), and upserts make re-running any hour safe.
/// </remarks>
public sealed class ResultMaintenance(AppDbContext db, IOptions<RetentionOptions> options)
{
    private const string RollupSql = """
        WITH scoped AS (
            SELECT check_id, worker, date_trunc('hour', observed_at, 'UTC') AS hour_start, outcome, measurements
            FROM check_results
            WHERE observed_at >= {0} AND observed_at < {1}
        ),
        counts AS (
            SELECT check_id, worker, hour_start,
                   count(*) FILTER (WHERE outcome = 'Up') AS up,
                   count(*) FILTER (WHERE outcome = 'Down') AS down,
                   count(*) FILTER (WHERE outcome = 'Error') AS error
            FROM scoped
            GROUP BY check_id, worker, hour_start
        ),
        dims AS (
            SELECT s.check_id, s.worker, s.hour_start, m.key,
                   jsonb_build_object(
                       'Min', min(m.value::float8),
                       'Avg', avg(m.value::float8),
                       'Max', max(m.value::float8),
                       'P95', percentile_cont(0.95) WITHIN GROUP (ORDER BY m.value::float8),
                       'Count', count(*)) AS stats
            FROM scoped s, jsonb_each_text(s.measurements) m
            GROUP BY s.check_id, s.worker, s.hour_start, m.key
        ),
        dims_by_hour AS (
            SELECT check_id, worker, hour_start, jsonb_object_agg(key, stats) AS dimensions
            FROM dims
            GROUP BY check_id, worker, hour_start
        )
        INSERT INTO check_result_rollups (check_id, worker, hour_start, up, down, error, dimensions)
        SELECT c.check_id, c.worker, c.hour_start, c.up, c.down, c.error, coalesce(d.dimensions, '{{}}'::jsonb)
        FROM counts c
        LEFT JOIN dims_by_hour d USING (check_id, worker, hour_start)
        ON CONFLICT (check_id, worker, hour_start) DO UPDATE
        SET up = excluded.up, down = excluded.down, error = excluded.error, dimensions = excluded.dimensions
        """;

    /// <summary>Runs rollups and retention once, as of <paramref name="now"/>.</summary>
    public async Task<MaintenanceReport> RunOnceAsync(DateTimeOffset now, CancellationToken ct)
    {
        var opts = options.Value;
        var currentHour = TruncateToHour(now);

        var from = await RollupStartAsync(opts.RollupLookback, ct);
        var written = 0;
        if (from is not null && from < currentHour)
        {
            written = await db.Database.ExecuteSqlRawAsync(RollupSql, [from.Value, currentHour], ct);
        }

        var resultsDeleted = await DeleteInBatchesAsync(
            "DELETE FROM check_results WHERE id IN (SELECT id FROM check_results WHERE observed_at < {0} LIMIT {1})",
            now - opts.RawResults, opts.DeleteBatchSize, ct);
        var rollupsDeleted = await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM check_result_rollups WHERE hour_start < {0}", [now - opts.Rollups], ct);

        return new MaintenanceReport(from, written, resultsDeleted, rollupsDeleted);
    }

    private async Task<DateTimeOffset?> RollupStartAsync(TimeSpan lookback, CancellationToken ct)
    {
        var latest = await db.CheckResultRollups.MaxAsync(r => (DateTimeOffset?)r.HourStart, ct);
        if (latest is not null)
        {
            return latest.Value - lookback;
        }

        var oldest = await db.CheckResults.MinAsync(r => (DateTimeOffset?)r.ObservedAt, ct);
        return oldest is null ? null : TruncateToHour(oldest.Value);
    }

    private async Task<int> DeleteInBatchesAsync(string sql, DateTimeOffset cutoff, int batchSize, CancellationToken ct)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await db.Database.ExecuteSqlRawAsync(sql, [cutoff, batchSize], ct);
            total += deleted;
        }
        while (deleted == batchSize);

        return total;
    }

    private static DateTimeOffset TruncateToHour(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }
}
