namespace Vigia.Application.Services;

/// <summary>
/// A dependency as exposed by the API.
/// </summary>
/// <param name="Service">Slug of the service depended on.</param>
/// <param name="Mode"><c>blocking</c>, <c>soft</c> or <c>advisory</c>.</param>
public sealed record DependencyDto(string Service, string Mode);
