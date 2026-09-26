using Vigia.Domain.Results;

namespace Vigia.Application.Checks.ListCheckRollups;

/// <summary>
/// Hourly summary of a check's results from one agent.
/// </summary>
/// <param name="Agent">Agent that produced the results.</param>
/// <param name="HourStart">Start of the hour (UTC).</param>
/// <param name="Up">Probes that were up.</param>
/// <param name="Down">Probes that were down.</param>
/// <param name="Error">Probes that could not run.</param>
/// <param name="Dimensions">Per-dimension min, avg, max, p95 and count.</param>
public sealed record CheckRollupDto(
    string Agent,
    DateTimeOffset HourStart,
    long Up,
    long Down,
    long Error,
    IReadOnlyDictionary<string, DimensionStats> Dimensions);
