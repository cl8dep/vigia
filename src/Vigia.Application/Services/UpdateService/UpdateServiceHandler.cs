using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;

namespace Vigia.Application.Services.UpdateService;

/// <summary>
/// Handles <see cref="UpdateServiceCommand"/>.
/// </summary>
public sealed class UpdateServiceHandler(IAppDbContext db, IServiceHealthUpdater health) : ICommandHandler<UpdateServiceCommand, ServiceDto>
{
    /// <inheritdoc />
    public async ValueTask<ServiceDto> Handle(UpdateServiceCommand command, CancellationToken ct)
    {
        var service = await db.Services.Include(s => s.Dependencies).SingleOrDefaultAsync(s => s.Slug == command.Slug, ct)
            ?? throw new NotFoundException("service", command.Slug);

        await ServiceSpecApplier.ApplyAsync(service, command.Slug, command.Spec, db, ct);
        await db.SaveChangesAsync(ct);

        // Membership, alerts or the graph may have changed.
        await health.RecomputeAsync(ct);
        return (await ServiceViews.BuildAsync(db, [service], ct))[0];
    }
}
