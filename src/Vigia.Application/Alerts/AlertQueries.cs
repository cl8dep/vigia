using System.Text.Json;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Alerts;

namespace Vigia.Application.Alerts;

/// <summary>
/// Projection shared by the alert queries.
/// </summary>
public static class AlertQueries
{
    /// <summary>Alerts joined with their rule and check slugs, newest first.</summary>
    public static IQueryable<AlertRow> Rows(IAppDbContext db, IQueryable<Alert> alerts)
    {
        // Order before the record projection; EF cannot translate members of a constructed record.
        return alerts
            .Join(db.Rules, a => a.RuleId, r => r.Id, (a, r) => new { Alert = a, Rule = r.Slug })
            .Join(db.Checks, x => x.Alert.CheckId, c => c.Id, (x, c) => new { x.Alert, x.Rule, Check = c.Slug })
            .OrderByDescending(x => x.Alert.FiredAt)
            .Select(x => new AlertRow(x.Alert, x.Rule, x.Check));
    }

    /// <summary>Maps a row to the API shape.</summary>
    public static AlertDto ToDto(AlertRow row)
    {
        var a = row.Alert;
        return new AlertDto(
            a.Id,
            row.Rule,
            row.Check,
            JsonNamingPolicy.CamelCase.ConvertName(a.Severity.ToString()),
            JsonNamingPolicy.CamelCase.ConvertName(a.State.ToString()),
            a.Message,
            a.FiredAt,
            a.LastSeenAt,
            a.ResolvedAt,
            a.Occurrences);
    }
}
