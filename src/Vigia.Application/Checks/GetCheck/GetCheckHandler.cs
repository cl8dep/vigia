using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;

namespace Vigia.Application.Checks.GetCheck;

/// <summary>
/// Handles <see cref="GetCheckQuery"/>.
/// </summary>
public sealed class GetCheckHandler(IAppDbContext db, IPluginRegistry registry) : IQueryHandler<GetCheckQuery, CheckDto>
{
    /// <inheritdoc />
    public async ValueTask<CheckDto> Handle(GetCheckQuery query, CancellationToken ct)
    {
        var check = await db.Checks.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == query.Slug, ct)
            ?? throw new NotFoundException("check", query.Slug);

        registry.TryGetCheck(check.Plugin, out var plugin);
        return CheckDto.From(check, plugin);
    }
}
