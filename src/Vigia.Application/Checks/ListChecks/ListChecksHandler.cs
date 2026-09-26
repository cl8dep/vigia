using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;

namespace Vigia.Application.Checks.ListChecks;

/// <summary>
/// Handles <see cref="ListChecksQuery"/>.
/// </summary>
public sealed class ListChecksHandler(IAppDbContext db, IPluginRegistry registry) : IQueryHandler<ListChecksQuery, IReadOnlyList<CheckDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CheckDto>> Handle(ListChecksQuery query, CancellationToken ct)
    {
        var checks = await db.Checks.AsNoTracking().OrderBy(c => c.Slug).ToListAsync(ct);
        return checks
            .Select(c => CheckDto.From(c, registry.TryGetCheck(c.Plugin, out var plugin) ? plugin : null))
            .ToList();
    }
}
