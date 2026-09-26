using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Workers.Heartbeat;

/// <summary>
/// Handles <see cref="HeartbeatCommand"/>. Returns the worker as the control plane sees it.
/// </summary>
public sealed class HeartbeatHandler(IAppDbContext db, TimeProvider time) : ICommandHandler<HeartbeatCommand, WorkerDto>
{
    /// <inheritdoc />
    public async ValueTask<WorkerDto> Handle(HeartbeatCommand command, CancellationToken ct)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Id == command.WorkerId, ct)
            ?? throw new UnauthorizedException("Unknown worker.");

        var now = time.GetUtcNow();
        worker.Seen(now, command.Version);
        await db.SaveChangesAsync(ct);
        return WorkerDto.From(worker, now);
    }
}
