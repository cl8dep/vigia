using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Workers.DeleteWorker;

/// <summary>
/// Handles <see cref="DeleteWorkerCommand"/>.
/// </summary>
public sealed class DeleteWorkerHandler(IAppDbContext db) : ICommandHandler<DeleteWorkerCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(DeleteWorkerCommand command, CancellationToken ct)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Slug == command.Slug, ct)
            ?? throw new NotFoundException("worker", command.Slug);

        if (worker.BuiltIn)
        {
            throw new ValidationException("slug", "The built-in worker cannot be deleted; disable it with Worker:BuiltInEnabled.");
        }

        db.Workers.Remove(worker);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
