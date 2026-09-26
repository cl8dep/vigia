using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Alerts.GetAlert;

/// <summary>
/// Handles <see cref="GetAlertQuery"/>.
/// </summary>
public sealed class GetAlertHandler(IAppDbContext db) : IQueryHandler<GetAlertQuery, AlertDto>
{
    /// <inheritdoc />
    public async ValueTask<AlertDto> Handle(GetAlertQuery query, CancellationToken ct)
    {
        var row = await AlertQueries.Rows(db, db.Alerts.AsNoTracking().Where(a => a.Id == query.Id)).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("alert", query.Id.ToString());
        return AlertQueries.ToDto(row);
    }
}
