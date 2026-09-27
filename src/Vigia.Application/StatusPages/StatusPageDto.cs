namespace Vigia.Application.StatusPages;

/// <summary>
/// A status page as exposed to admins (the public view is <see cref="PublicStatus.PublicStatusDto"/>).
/// </summary>
/// <param name="Slug">Stable identity; also the public URL <c>/status/{slug}</c>.</param>
/// <param name="Title">Public title.</param>
/// <param name="Description">Public description.</param>
/// <param name="Tags">Tags.</param>
/// <param name="Components">Components in display order.</param>
public sealed record StatusPageDto(
    string Slug,
    string Title,
    string? Description,
    IReadOnlyDictionary<string, string?> Tags,
    IReadOnlyList<ComponentSpec> Components);
