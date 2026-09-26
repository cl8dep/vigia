namespace Vigia.Application.Auth;

/// <summary>
/// Tokens issued on sign-in and refresh.
/// </summary>
/// <param name="TokenType">Always <c>Bearer</c>.</param>
/// <param name="AccessToken">Token to send in the <c>Authorization</c> header.</param>
/// <param name="ExpiresIn">Access token lifetime in seconds.</param>
/// <param name="RefreshToken">Token to get a new access token from <c>/api/v1/auth/refresh</c> after it expires.</param>
public sealed record AuthTokens(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
