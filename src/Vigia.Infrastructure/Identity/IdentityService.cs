using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vigia.Application.Auth;
using Vigia.Application.Common.Exceptions;

namespace Vigia.Infrastructure.Identity;

/// <summary>
/// <see cref="IIdentityService"/> on ASP.NET Core Identity. Issues the same bearer tokens the Identity bearer handler validates.
/// </summary>
public sealed class IdentityService(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    TimeProvider time) : IIdentityService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefreshToken = "Invalid or expired refresh token. Sign in again.";

    /// <inheritdoc />
    public async Task<bool> AnyUsersAsync(CancellationToken ct)
    {
        return await users.Users.AnyAsync(ct);
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateUserAsync(string email, string password, CancellationToken ct)
    {
        var user = new AppUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).Distinct().ToArray());
            throw new ValidationException(errors);
        }

        return new UserDto(user.Id, email);
    }

    /// <inheritdoc />
    public async Task<AuthTokens> SignInAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email) ?? throw new UnauthorizedException(InvalidCredentials);

        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            throw new UnauthorizedException("Too many failed attempts. Try again later.");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        return Issue(await signIn.CreateUserPrincipalAsync(user));
    }

    /// <inheritdoc />
    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var ticket = Options.RefreshTokenProtector.Unprotect(refreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expires || expires < time.GetUtcNow())
        {
            throw new UnauthorizedException(InvalidRefreshToken);
        }

        // Rejects tokens issued before a password change or any other security stamp update.
        var user = await signIn.ValidateSecurityStampAsync(ticket.Principal)
            ?? throw new UnauthorizedException(InvalidRefreshToken);

        return Issue(await signIn.CreateUserPrincipalAsync(user));
    }

    private BearerTokenOptions Options
    {
        get { return bearerOptions.Get(IdentityConstants.BearerScheme); }
    }

    private AuthTokens Issue(System.Security.Claims.ClaimsPrincipal principal)
    {
        var options = Options;
        var now = time.GetUtcNow();

        var access = new AuthenticationTicket(
            principal,
            new AuthenticationProperties { ExpiresUtc = now + options.BearerTokenExpiration },
            $"{IdentityConstants.BearerScheme}:AccessToken");
        var refresh = new AuthenticationTicket(
            principal,
            new AuthenticationProperties { ExpiresUtc = now + options.RefreshTokenExpiration },
            $"{IdentityConstants.BearerScheme}:RefreshToken");

        return new AuthTokens(
            "Bearer",
            options.BearerTokenProtector.Protect(access),
            (long)options.BearerTokenExpiration.TotalSeconds,
            options.RefreshTokenProtector.Protect(refresh));
    }
}
