using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Alerts;

namespace Vigia.Application.Alerts.ListAlerts;

/// <summary>
/// Handles <see cref="ListAlertsQuery"/>.
/// </summary>
public sealed class ListAlertsHandler(IAppDbContext db) : IQueryHandler<ListAlertsQuery, IReadOnlyList<AlertDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AlertDto>> Handle(ListAlertsQuery query, CancellationToken ct)
    {
        if (query.Limit is < 1 or > 500)
        {
            throw new ValidationException("limit", "Limit must be between 1 and 500.");
        }

        var alerts = db.Alerts.AsNoTracking();
        if (query.State is not null)
        {
            if (!Enum.TryParse<AlertState>(query.State, ignoreCase: true, out var state))
            {
                throw new ValidationException("state", "Use firing or resolved.");
            }

            alerts = alerts.Where(a => a.State == state);
        }

        if (query.Check is not null)
        {
            alerts = alerts.Where(a => db.Checks.Any(c => c.Id == a.CheckId && c.Slug == query.Check));
        }

        var rows = await AlertQueries.Rows(db, alerts).Take(query.Limit).ToListAsync(ct);
        return rows.Select(AlertQueries.ToDto).ToList();
    }
}
