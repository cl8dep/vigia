using Microsoft.EntityFrameworkCore;
using Vigia.Application.Agents;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Rules;
using Vigia.Domain.Results;

namespace Vigia.Application.Results;

/// <summary>
/// The single entry point for results: stores the result and evaluates rules in one save, so an alert never
/// exists without the result that caused it (or the other way around).
/// </summary>
public sealed class ResultIngestor(IAppDbContext db, RuleEvaluator rules)
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
            record.Agent,
            Enum.Parse<ResultOutcome>(record.Outcome.ToString()),
            record.Measurements,
            record.Message,
            record.DurationMs,
            record.ObservedAt);
        db.CheckResults.Add(result);

        await rules.EvaluateAsync(check, result, ct);
        await db.SaveChangesAsync(ct);
    }
}
