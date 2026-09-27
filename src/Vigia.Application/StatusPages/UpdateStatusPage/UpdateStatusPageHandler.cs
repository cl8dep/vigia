using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.StatusPages.UpdateStatusPage;

/// <summary>
/// Handles <see cref="UpdateStatusPageCommand"/>.
/// </summary>
public sealed class UpdateStatusPageHandler(IAppDbContext db) : ICommandHandler<UpdateStatusPageCommand, StatusPageDto>
{
    /// <inheritdoc />
    public async ValueTask<StatusPageDto> Handle(UpdateStatusPageCommand command, CancellationToken ct)
    {
        var page = await db.StatusPages.Include(p => p.Components).SingleOrDefaultAsync(p => p.Slug == command.Slug, ct)
            ?? throw new NotFoundException("status page", command.Slug);

        await StatusPageViews.ApplyAsync(page, command.Slug, command.Spec, db, ct);
        await db.SaveChangesAsync(ct);
        return await StatusPageViews.ToDtoAsync(page, db, ct);
    }
}
