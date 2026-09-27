using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Services.ListServices;

/// <summary>
/// Handles <see cref="ListServicesQuery"/>.
/// </summary>
public sealed class ListServicesHandler(IAppDbContext db) : IQueryHandler<ListServicesQuery, IReadOnlyList<ServiceDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ServiceDto>> Handle(ListServicesQuery query, CancellationToken ct)
    {
        var services = await db.Services.AsNoTracking().Include(s => s.Dependencies).OrderBy(s => s.Slug).ToListAsync(ct);
        return await ServiceViews.BuildAsync(db, services, ct);
    }
}
