using Vigia.Domain.Health;

namespace Vigia.Application.Health;

/// <summary>
/// Availability of a service over one day.
/// </summary>
/// <param name="Date">Day (UTC).</param>
/// <param name="State">Worst state seen that day, or null when there is no data for the day.</param>
/// <param name="Uptime">Share of the known time that was available, 0 to 1; null when there is no data.</param>
public sealed record DayAvailability(DateOnly Date, HealthState? State, double? Uptime);
