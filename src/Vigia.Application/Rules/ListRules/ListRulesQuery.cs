using Mediator;

namespace Vigia.Application.Rules.ListRules;

/// <summary>
/// Lists all rules ordered by slug.
/// </summary>
public sealed record ListRulesQuery : IQuery<IReadOnlyList<RuleDto>>;
