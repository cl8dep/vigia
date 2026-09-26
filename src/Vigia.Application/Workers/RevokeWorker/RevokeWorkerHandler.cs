using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Common;

namespace Vigia.Application.Workers.RevokeWorker;

/// <summary>
/// Handles <see cref="RevokeWorkerCommand"/>.
/// </summary>
public sealed class RevokeWorkerHandler(IAppDbContext db, TimeProvider time) : ICommandHandler<RevokeWorkerCommand, WorkerDto>
{
    /// <inheritdoc />
    public async ValueTask<WorkerDto> Handle(RevokeWorkerCommand command, CancellationToken ct)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Slug == command.Slug, ct)
            ?? throw new NotFoundException("worker", command.Slug);

        try
        {
            worker.Revoke();
        }
        catch (DomainException ex)
        {
            throw new ValidationException("slug", ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return WorkerDto.From(worker, time.GetUtcNow());
    }
}
