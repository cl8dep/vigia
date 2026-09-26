using Mediator;

namespace Vigia.Application.Rules.CreateRule;

/// <summary>
/// Creates a rule.
/// </summary>
/// <param name="Slug">Stable identity, unique.</param>
/// <param name="Spec">Rule definition.</param>
public sealed record CreateRuleCommand(string Slug, RuleSpec Spec) : ICommand<RuleDto>;
