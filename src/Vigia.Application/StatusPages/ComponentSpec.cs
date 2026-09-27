namespace Vigia.Application.StatusPages;

/// <summary>
/// A status page component as clients write it.
/// </summary>
/// <param name="Service">Slug of the service shown.</param>
/// <param name="Name">Public name.</param>
/// <param name="Group">Public group heading, or null.</param>
public sealed record ComponentSpec(string Service, string Name, string? Group);
