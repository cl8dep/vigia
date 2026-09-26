namespace Vigia.Application.Common.Exceptions;

/// <summary>
/// The requested entity does not exist.
/// </summary>
/// <param name="kind">Entity kind, for example <c>check</c>.</param>
/// <param name="key">Slug or id that was looked up.</param>
public sealed class NotFoundException(string kind, string key) : Exception($"{kind} '{key}' was not found.");
