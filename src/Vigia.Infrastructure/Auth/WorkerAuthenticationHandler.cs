using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vigia.Application.Common.Security;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Auth;

/// <summary>
/// Authenticates <c>Authorization: Worker &lt;credential&gt;</c> by the SHA-256 of the credential.
/// Revoked or deleted workers fail on their next request.
/// </summary>
public sealed class WorkerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AppDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        var prefix = WorkerAuthentication.Scheme + " ";
        if (!header.StartsWith(prefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = SecretTokens.Hash(header[prefix.Length..].Trim());
        var worker = await db.Workers.AsNoTracking()
            .Where(w => w.CredentialHash == hash)
            .Select(w => new { w.Id, w.Slug })
            .SingleOrDefaultAsync(Context.RequestAborted);
        if (worker is null)
        {
            return AuthenticateResult.Fail("Invalid worker credential.");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, worker.Id.ToString()),
                new Claim(ClaimTypes.Name, worker.Slug),
                new Claim(WorkerAuthentication.SlugClaim, worker.Slug),
            ],
            Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
