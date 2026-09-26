namespace Vigia.Domain.Results;

/// <summary>
/// Statistics of one dimension over a rollup period.
/// </summary>
/// <param name="Min">Smallest value.</param>
/// <param name="Avg">Mean value.</param>
/// <param name="Max">Largest value.</param>
/// <param name="P95">95th percentile.</param>
/// <param name="Count">Number of measurements.</param>
public sealed record DimensionStats(double Min, double Avg, double Max, double P95, long Count);
