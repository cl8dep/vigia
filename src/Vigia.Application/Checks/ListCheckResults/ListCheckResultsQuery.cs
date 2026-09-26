using Mediator;

namespace Vigia.Application.Checks.ListCheckResults;

/// <summary>
/// Latest results of a check, newest first.
/// </summary>
/// <param name="Slug">Check slug.</param>
/// <param name="Limit">Maximum number of results, 1 to <see cref="MaxLimit"/>.</param>
public sealed record ListCheckResultsQuery(string Slug, int Limit) : IQuery<IReadOnlyList<CheckResultDto>>
{
    /// <summary>Largest page size accepted.</summary>
    public const int MaxLimit = 500;
}
