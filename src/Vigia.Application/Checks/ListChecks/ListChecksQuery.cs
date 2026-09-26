using Mediator;

namespace Vigia.Application.Checks.ListChecks;

/// <summary>
/// Lists all checks ordered by slug.
/// </summary>
public sealed record ListChecksQuery : IQuery<IReadOnlyList<CheckDto>>;
