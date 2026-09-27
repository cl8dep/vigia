using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.StatusPages.GetStatusPage;

/// <summary>
/// Handles <see cref="GetStatusPageQuery"/>.
/// </summary>
public sealed class GetStatusPageHandler(IAppDbContext db) : IQueryHandler<GetStatusPageQuery, StatusPageDto>
{
    /// <inheritdoc />
    public async ValueTask<StatusPageDto> Handle(GetStatusPageQuery query, CancellationToken ct)
    {
        var page = await db.StatusPages.AsNoTracking().Include(p => p.Components).SingleOrDefaultAsync(p => p.Slug == query.Slug, ct)
            ?? throw new NotFoundException("status page", query.Slug);
        return await StatusPageViews.ToDtoAsync(page, db, ct);
    }
}
