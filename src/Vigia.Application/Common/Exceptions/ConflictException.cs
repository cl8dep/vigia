namespace Vigia.Application.Common.Exceptions;

/// <summary>
/// The operation conflicts with existing state, for example a duplicate slug.
/// </summary>
/// <param name="message">What conflicts.</param>
public sealed class ConflictException(string message) : Exception(message);
