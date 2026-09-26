using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Security;

namespace Vigia.Application.Workers.Enroll;

/// <summary>
/// Handles <see cref="EnrollCommand"/>. The token is single use; enrolling again needs a new one.
/// </summary>
public sealed class EnrollHandler(IAppDbContext db, TimeProvider time) : ICommandHandler<EnrollCommand, EnrollmentDto>
{
    /// <summary>Prefix of worker credentials, so a leaked one is recognizable.</summary>
    public const string CredentialPrefix = "vw_";

    /// <inheritdoc />
    public async ValueTask<EnrollmentDto> Handle(EnrollCommand command, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var hash = SecretTokens.Hash(command.Token ?? string.Empty);
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.EnrollmentTokenHash == hash, ct);
        if (worker is null || !worker.CanEnroll(now))
        {
            throw new UnauthorizedException("Invalid or expired enrollment token.");
        }

        var (credential, credentialHash) = SecretTokens.Create(CredentialPrefix);
        worker.Enroll(credentialHash, now);
        worker.Seen(now, command.Version);
        await db.SaveChangesAsync(ct);
        return new EnrollmentDto(worker.Slug, credential);
    }
}
