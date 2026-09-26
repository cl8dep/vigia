namespace Vigia.Application.Auth;

/// <summary>
/// A user as exposed by the API.
/// </summary>
/// <param name="Id">User id.</param>
/// <param name="Email">Email, also used as the login name.</param>
public sealed record UserDto(Guid Id, string Email);
