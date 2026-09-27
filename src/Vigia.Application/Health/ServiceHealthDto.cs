namespace Vigia.Application.Health;

/// <summary>
/// Stored health of a service.
/// </summary>
/// <param name="State"><c>operational</c>, <c>degraded</c>, <c>partial-outage</c>, <c>down</c>, <c>maintenance</c> or <c>unknown</c>.</param>
/// <param name="Reason">Why, for people.</param>
/// <param name="Since">When the current state started.</param>
/// <param name="Partitions">State per partition, when the service is partitioned.</param>
/// <param name="EvaluatedAt">Last recomputation.</param>
public sealed record ServiceHealthDto(string State, string Reason, DateTimeOffset Since, IReadOnlyDictionary<string, string> Partitions, DateTimeOffset EvaluatedAt);
