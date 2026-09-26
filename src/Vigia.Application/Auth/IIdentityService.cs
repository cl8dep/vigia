namespace Vigia.Application.Auth;

/// <summary>
/// User accounts and credentials. Implemented with ASP.NET Core Identity in Infrastructure.
/// </summary>
public interface IIdentityService
{
    /// <summary>Whether any user exists. Used to allow the bootstrap sign-up.</summary>
    Task<bool> AnyUsersAsync(CancellationToken ct);

    /// <summary>Creates a user.</summary>
    /// <exception cref="Common.Exceptions.ValidationException">Email taken or invalid, or password too weak.</exception>
    Task<UserDto> CreateUserAsync(string email, string password, CancellationToken ct);

    /// <summary>Checks credentials and issues tokens.</summary>
    /// <exception cref="Common.Exceptions.UnauthorizedException">Wrong credentials or account locked out.</exception>
    Task<AuthTokens> SignInAsync(string email, string password, CancellationToken ct);

    /// <summary>Validates a refresh token and issues new tokens.</summary>
    /// <exception cref="Common.Exceptions.UnauthorizedException">
    /// The token is invalid or expired, or the user's security stamp changed since it was issued (for example a password change).
    /// </exception>
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct);
}
