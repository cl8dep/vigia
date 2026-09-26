using Mediator;

namespace Vigia.Application.Rules.GetRule;

/// <summary>
/// Gets a rule by slug.
/// </summary>
/// <param name="Slug">Rule slug.</param>
public sealed record GetRuleQuery(string Slug) : IQuery<RuleDto>;
