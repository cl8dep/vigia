using Mediator;

namespace Vigia.Application.Rules.DeleteRule;

/// <summary>
/// Deletes a rule and its alerts.
/// </summary>
/// <param name="Slug">Rule to delete.</param>
public sealed record DeleteRuleCommand(string Slug) : ICommand;
