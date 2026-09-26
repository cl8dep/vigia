using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Security;
using Vigia.Domain.Common;
using Vigia.Domain.Workers;

namespace Vigia.Application.Workers.CreateWorker;

/// <summary>
/// Handles <see cref="CreateWorkerCommand"/>.
/// </summary>
public sealed class CreateWorkerHandler(IAppDbContext db, TimeProvider time) : ICommandHandler<CreateWorkerCommand, WorkerDto>
{
    /// <inheritdoc />
    public async ValueTask<WorkerDto> Handle(CreateWorkerCommand command, CancellationToken ct)
    {
        if (await db.Workers.AnyAsync(w => w.Slug == command.Slug, ct))
        {
            throw new ConflictException($"A worker with slug '{command.Slug}' already exists.");
        }

        Worker worker;
        try
        {
            worker = new Worker(command.Slug, command.Spec.Name ?? command.Slug, null, builtIn: false, ManagedBy.Ui);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("slug", ex.Message);
        }

        WorkerSpecApplier.Apply(worker, command.Slug, command.Spec);

        var now = time.GetUtcNow();
        var (token, hash) = SecretTokens.Create(EnrollmentTokenPrefix);
        worker.IssueEnrollmentToken(hash, now);

        db.Workers.Add(worker);
        await db.SaveChangesAsync(ct);
        return WorkerDto.From(worker, now, token);
    }

    /// <summary>Prefix of enrollment tokens, so a leaked one is recognizable.</summary>
    public const string EnrollmentTokenPrefix = "vwe_";
}
