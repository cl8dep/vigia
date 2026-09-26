using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Placement;
using Vigia.Application.Plugins;

namespace Vigia.Application.Checks.GetCheck;

/// <summary>
/// Handles <see cref="GetCheckQuery"/>.
/// </summary>
public sealed class GetCheckHandler(IAppDbContext db, IPluginRegistry registry, TimeProvider time) : IQueryHandler<GetCheckQuery, CheckDto>
{
    /// <inheritdoc />
    public async ValueTask<CheckDto> Handle(GetCheckQuery query, CancellationToken ct)
    {
        var check = await db.Checks.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == query.Slug, ct)
            ?? throw new NotFoundException("check", query.Slug);

        registry.TryGetCheck(check.Plugin, out var plugin);
        var workers = await db.Workers.AsNoTracking().ToListAsync(ct);
        return CheckDto.From(check, plugin, CheckPlacement.Evaluate(check, plugin, workers, time.GetUtcNow()));
    }
}
