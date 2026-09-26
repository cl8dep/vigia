using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Placement;
using Vigia.Application.Plugins;

namespace Vigia.Application.Checks.ListChecks;

/// <summary>
/// Handles <see cref="ListChecksQuery"/>.
/// </summary>
public sealed class ListChecksHandler(IAppDbContext db, IPluginRegistry registry, TimeProvider time) : IQueryHandler<ListChecksQuery, IReadOnlyList<CheckDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CheckDto>> Handle(ListChecksQuery query, CancellationToken ct)
    {
        var checks = await db.Checks.AsNoTracking().OrderBy(c => c.Slug).ToListAsync(ct);
        var workers = await db.Workers.AsNoTracking().ToListAsync(ct);
        var now = time.GetUtcNow();
        return checks
            .Select(c =>
            {
                var plugin = registry.TryGetCheck(c.Plugin, out var found) ? found : null;
                return CheckDto.From(c, plugin, CheckPlacement.Evaluate(c, plugin, workers, now));
            })
            .ToList();
    }
}
