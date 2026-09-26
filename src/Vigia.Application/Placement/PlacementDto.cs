namespace Vigia.Application.Placement;

/// <summary>
/// Where a check can run right now.
/// </summary>
/// <param name="State">
/// <c>ok</c>; <c>unschedulable</c> when no registered worker matches (fix the check); <c>workers-offline</c> when
/// matching workers exist but none is online (transient). Never a reason to mark the target down.
/// </param>
/// <param name="Eligible">Slugs of registered workers that may run the check.</param>
/// <param name="Online">Slugs of eligible workers that are online.</param>
public sealed record PlacementDto(string State, IReadOnlyList<string> Eligible, IReadOnlyList<string> Online);
