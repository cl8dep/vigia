using Mediator;

namespace Vigia.Application.Rules.UpdateRule;

/// <summary>
/// Replaces a rule's definition. Firing alerts stay open and are re-evaluated with the new definition.
/// </summary>
/// <param name="Slug">Rule to update.</param>
/// <param name="Spec">New definition.</param>
public sealed record UpdateRuleCommand(string Slug, RuleSpec Spec) : ICommand<RuleDto>;
