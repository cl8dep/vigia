using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Security;
using Vigia.Application.Workers.CreateWorker;
using Vigia.Domain.Common;

namespace Vigia.Application.Workers.IssueEnrollmentToken;

/// <summary>
/// Handles <see cref="IssueEnrollmentTokenCommand"/>.
/// </summary>
public sealed class IssueEnrollmentTokenHandler(IAppDbContext db, TimeProvider time) : ICommandHandler<IssueEnrollmentTokenCommand, WorkerDto>
{
    /// <inheritdoc />
    public async ValueTask<WorkerDto> Handle(IssueEnrollmentTokenCommand command, CancellationToken ct)
    {
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Slug == command.Slug, ct)
            ?? throw new NotFoundException("worker", command.Slug);

        var now = time.GetUtcNow();
        var (token, hash) = SecretTokens.Create(CreateWorkerHandler.EnrollmentTokenPrefix);
        try
        {
            worker.IssueEnrollmentToken(hash, now);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("slug", ex.Message);
        }

        await db.SaveChangesAsync(ct);
        return WorkerDto.From(worker, now, token);
    }
}
