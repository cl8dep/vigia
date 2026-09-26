using Mediator;

namespace Vigia.Application.Auth.Refresh;

/// <summary>
/// Exchanges a refresh token for new tokens.
/// </summary>
/// <param name="RefreshToken">Refresh token from a previous sign-in or refresh.</param>
public sealed record RefreshCommand(string RefreshToken) : ICommand<AuthTokens>;
