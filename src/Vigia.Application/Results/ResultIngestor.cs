using Microsoft.EntityFrameworkCore;
using Vigia.Application.Workers;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;
using Vigia.Application.Rules;
using Vigia.Domain.Results;

namespace Vigia.Application.Results;

/// <summary>
/// The single entry point for results: stores the result, evaluates rules and, when an alert opened or resolved,
/// recomputes service health, all in the caller's transaction, so none of them exists without its cause.
/// </summary>
public sealed class ResultIngestor(IAppDbContext db, RuleEvaluator rules, IServiceHealthUpdater health)
{
    /// <summary>Stores and evaluates one probe result. Results for deleted checks are dropped.</summary>
    public async Task IngestAsync(ProbeRecord record, CancellationToken ct)
    {
        var check = await db.Checks.AsNoTracking().SingleOrDefaultAsync(c => c.Id == record.CheckId, ct);
        if (check is null)
        {
            return;
        }

        var result = new CheckResult(
            record.Id,
            record.CheckId,
            record.Worker,
            Enum.Parse<ResultOutcome>(record.Outcome.ToString()),
            record.Measurements,
            record.Message,
            record.DurationMs,
            record.ObservedAt);
        db.CheckResults.Add(result);

        var alertsChanged = await rules.EvaluateAsync(check, result, ct);
        await db.SaveChangesAsync(ct);

        if (alertsChanged)
        {
            await health.RecomputeAsync(ct);
        }
    }
}
