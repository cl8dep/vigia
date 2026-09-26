namespace Vigia.Check.Dns;

/// <summary>
/// What one resolver answered.
/// </summary>
/// <param name="Resolver">Resolver address, or <c>system</c>.</param>
/// <param name="Values">Normalized record values (lowercase, no trailing dot).</param>
/// <param name="Error">Why the query failed, or null.</param>
/// <param name="LatencyMs">Query time.</param>
internal sealed record ResolverAnswer(string Resolver, IReadOnlyList<string> Values, string? Error, double LatencyMs);
