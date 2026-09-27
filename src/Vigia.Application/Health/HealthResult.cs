using Vigia.Domain.Health;

namespace Vigia.Application.Health;

/// <summary>
/// Computed health of one service.
/// </summary>
/// <param name="State">Overall state after partitions and dependencies.</param>
/// <param name="Reason">Why, for people.</param>
/// <param name="Partitions">State per partition, when partitioned.</param>
public sealed record HealthResult(HealthState State, string Reason, IReadOnlyDictionary<string, HealthState> Partitions);
