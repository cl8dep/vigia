using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.StatusPages.ListStatusPages;

/// <summary>
/// Handles <see cref="ListStatusPagesQuery"/>.
/// </summary>
public sealed class ListStatusPagesHandler(IAppDbContext db) : IQueryHandler<ListStatusPagesQuery, IReadOnlyList<StatusPageDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<StatusPageDto>> Handle(ListStatusPagesQuery query, CancellationToken ct)
    {
        var pages = await db.StatusPages.AsNoTracking().Include(p => p.Components).OrderBy(p => p.Slug).ToListAsync(ct);
        var result = new List<StatusPageDto>();
        foreach (var page in pages)
        {
            result.Add(await StatusPageViews.ToDtoAsync(page, db, ct));
        }

        return result;
    }
}
