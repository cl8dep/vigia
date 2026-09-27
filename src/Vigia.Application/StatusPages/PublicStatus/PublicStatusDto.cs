namespace Vigia.Application.StatusPages.PublicStatus;

/// <summary>
/// What <c>GET /status/{slug}</c> returns, without authentication.
/// </summary>
/// <param name="Title">Page title.</param>
/// <param name="Description">Page description.</param>
/// <param name="State">Worst component state.</param>
/// <param name="Components">Components in display order.</param>
/// <param name="GeneratedAt">When this view was produced.</param>
public sealed record PublicStatusDto(string Title, string? Description, string State, IReadOnlyList<PublicComponentDto> Components, DateTimeOffset GeneratedAt);
