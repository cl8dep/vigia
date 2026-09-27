namespace Vigia.Application.Services;

/// <summary>
/// A dependency as clients write it.
/// </summary>
/// <param name="Service">Slug of the service depended on.</param>
/// <param name="Mode"><c>blocking</c>, <c>soft</c> or <c>advisory</c>. Default <c>blocking</c>.</param>
public sealed record DependencySpec(string Service, string? Mode);
