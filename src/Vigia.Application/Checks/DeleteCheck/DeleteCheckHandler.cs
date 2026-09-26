using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Checks.DeleteCheck;

/// <summary>
/// Handles <see cref="DeleteCheckCommand"/>. Results and rollups go with it (cascade).
/// </summary>
public sealed class DeleteCheckHandler(IAppDbContext db) : ICommandHandler<DeleteCheckCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(DeleteCheckCommand command, CancellationToken ct)
    {
        var check = await db.Checks.SingleOrDefaultAsync(c => c.Slug == command.Slug, ct)
            ?? throw new NotFoundException("check", command.Slug);

        db.Checks.Remove(check);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
