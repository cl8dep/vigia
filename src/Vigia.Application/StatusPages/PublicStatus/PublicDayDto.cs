namespace Vigia.Application.StatusPages.PublicStatus;

/// <summary>
/// One day in a component's availability bar.
/// </summary>
/// <param name="Date">Day (UTC), <c>yyyy-MM-dd</c>.</param>
/// <param name="State">Worst state that day, or null without data.</param>
/// <param name="Uptime">Availability, 0 to 1, or null without data.</param>
public sealed record PublicDayDto(string Date, string? State, double? Uptime);
