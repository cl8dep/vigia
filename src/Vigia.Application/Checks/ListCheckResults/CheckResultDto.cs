namespace Vigia.Application.Checks.ListCheckResults;

/// <summary>
/// A stored probe result as exposed by the API.
/// </summary>
/// <param name="Id">Result id.</param>
/// <param name="Worker">Worker that ran the probe.</param>
/// <param name="Outcome"><c>up</c>, <c>down</c> or <c>error</c>.</param>
/// <param name="Measurements">Dimension name to value.</param>
/// <param name="Message">Why the target is down or the probe failed.</param>
/// <param name="DurationMs">Probe duration in milliseconds.</param>
/// <param name="ObservedAt">When the probe started (UTC).</param>
public sealed record CheckResultDto(
    Guid Id,
    string Worker,
    string Outcome,
    IReadOnlyDictionary<string, double> Measurements,
    string? Message,
    double DurationMs,
    DateTimeOffset ObservedAt);
