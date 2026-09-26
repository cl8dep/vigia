using Mediator;

namespace Vigia.Application.Auth.SignIn;

/// <summary>
/// Signs in with email and password and returns a bearer token.
/// </summary>
/// <param name="Email">Email.</param>
/// <param name="Password">Password.</param>
public sealed record SignInCommand(string Email, string Password) : ICommand<AuthTokens>;
