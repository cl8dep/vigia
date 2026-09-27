using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;

namespace Vigia.Application.Checks.DeleteCheck;

/// <summary>
/// Handles <see cref="DeleteCheckCommand"/>. Results and rollups go with it (cascade).
/// </summary>
public sealed class DeleteCheckHandler(IAppDbContext db, IServiceHealthUpdater health) : ICommandHandler<DeleteCheckCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(DeleteCheckCommand command, CancellationToken ct)
    {
        var check = await db.Checks.SingleOrDefaultAsync(c => c.Slug == command.Slug, ct)
            ?? throw new NotFoundException("check", command.Slug);

        db.Checks.Remove(check);
        await db.SaveChangesAsync(ct);

        // Membership, alerts or the graph may have changed.
        await health.RecomputeAsync(ct);
        return Unit.Value;
    }
}
