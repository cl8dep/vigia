namespace Vigia.Application.Checks.ProbeCheck;

/// <summary>
/// Result of a single probe.
/// </summary>
/// <param name="Outcome"><c>up</c>, <c>down</c> or <c>error</c>.</param>
/// <param name="Measurements">Dimension name to value.</param>
/// <param name="Message">Why the target is down or the probe failed.</param>
/// <param name="Duration">Probe duration in milliseconds.</param>
public sealed record ProbeResultDto(
    string Outcome,
    IReadOnlyDictionary<string, double> Measurements,
    string? Message,
    double Duration);
