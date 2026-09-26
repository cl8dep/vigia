using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Workers.UpdateWorker;

/// <summary>
/// Handles <see cref="UpdateWorkerCommand"/>.
/// </summary>
public sealed class UpdateWorkerHandler(IAppDbContext db, TimeProvider time) : ICommandHandler<UpdateWorkerCommand, WorkerDto>
{
    /// <inheritdoc />
    public async ValueTask<WorkerDto> Handle(UpdateWorkerCommand command, CancellationToken ct)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Slug == command.Slug, ct)
            ?? throw new NotFoundException("worker", command.Slug);

        WorkerSpecApplier.Apply(worker, command.Slug, command.Spec);
        await db.SaveChangesAsync(ct);
        return WorkerDto.From(worker, time.GetUtcNow());
    }
}
