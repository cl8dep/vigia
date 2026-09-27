using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;

namespace Vigia.Application.Services.DeleteService;

/// <summary>
/// Handles <see cref="DeleteServiceCommand"/>.
/// </summary>
public sealed class DeleteServiceHandler(IAppDbContext db, IServiceHealthUpdater health) : ICommandHandler<DeleteServiceCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(DeleteServiceCommand command, CancellationToken ct)
    {
        var service = await db.Services.SingleOrDefaultAsync(s => s.Slug == command.Slug, ct)
            ?? throw new NotFoundException("service", command.Slug);

        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);

        // Membership, alerts or the graph may have changed.
        await health.RecomputeAsync(ct);
        return Unit.Value;
    }
}
