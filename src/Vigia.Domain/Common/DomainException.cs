namespace Vigia.Domain.Common;

/// <summary>
/// Thrown when an operation would break a domain invariant.
/// </summary>
/// <param name="message">Which invariant was broken.</param>
public sealed class DomainException(string message) : Exception(message);
