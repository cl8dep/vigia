using Mediator;

namespace Vigia.Application.Auth.SignUp;

/// <summary>
/// Creates an account with email and password.
/// </summary>
/// <param name="Email">Email, also the login name.</param>
/// <param name="Password">Password; must meet the Identity password policy.</param>
public sealed record SignUpCommand(string Email, string Password) : ICommand<UserDto>;
