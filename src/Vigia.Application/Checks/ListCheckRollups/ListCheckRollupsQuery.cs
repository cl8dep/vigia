using Mediator;

namespace Vigia.Application.Checks.ListCheckRollups;

/// <summary>
/// Hourly rollups of a check in a time range, oldest first.
/// </summary>
/// <param name="Slug">Check slug.</param>
/// <param name="From">Range start (inclusive). Defaults to 24 hours before <paramref name="To"/>.</param>
/// <param name="To">Range end (exclusive). Defaults to now.</param>
public sealed record ListCheckRollupsQuery(string Slug, DateTimeOffset? From, DateTimeOffset? To) : IQuery<IReadOnlyList<CheckRollupDto>>
{
    /// <summary>Widest range accepted in one request.</summary>
    public static readonly TimeSpan MaxRange = TimeSpan.FromDays(90);
}
