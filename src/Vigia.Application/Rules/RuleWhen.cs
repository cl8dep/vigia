namespace Vigia.Application.Rules;

/// <summary>
/// Rule condition as written by clients: either <c>{ "outcome": "down" }</c> or
/// <c>{ "dimension": "latency", "above": 800 }</c> / <c>{ "dimension": "days-to-expiry", "below": 14 }</c>.
/// </summary>
/// <param name="Outcome">Only <c>down</c>.</param>
/// <param name="Dimension">Dimension name declared by the check's plugin.</param>
/// <param name="Above">Fires when the dimension is above this value.</param>
/// <param name="Below">Fires when the dimension is below this value.</param>
public sealed record RuleWhen(string? Outcome, string? Dimension, double? Above, double? Below);
