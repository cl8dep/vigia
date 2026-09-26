namespace Vigia.Application.Common.Exceptions;

/// <summary>
/// Credentials are missing or wrong.
/// </summary>
/// <param name="message">Safe message for the client. Never says which part of the credentials was wrong.</param>
public sealed class UnauthorizedException(string message) : Exception(message);
