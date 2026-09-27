using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.StatusPages.DeleteStatusPage;

/// <summary>
/// Handles <see cref="DeleteStatusPageCommand"/>.
/// </summary>
public sealed class DeleteStatusPageHandler(IAppDbContext db) : ICommandHandler<DeleteStatusPageCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(DeleteStatusPageCommand command, CancellationToken ct)
    {
        var page = await db.StatusPages.SingleOrDefaultAsync(p => p.Slug == command.Slug, ct)
            ?? throw new NotFoundException("status page", command.Slug);

        db.StatusPages.Remove(page);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
