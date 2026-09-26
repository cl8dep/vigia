namespace Vigia.Application.Common.Exceptions;

/// <summary>
/// The operation is not allowed in the current state or for the current user.
/// </summary>
/// <param name="message">Why it is not allowed.</param>
public sealed class ForbiddenException(string message) : Exception(message);
