using System.Text.Json;
using Vigia.Domain.Rules;

namespace Vigia.Application.Rules;

/// <summary>
/// A rule as exposed by the API, in the same shape clients write it.
/// </summary>
/// <param name="Slug">Stable identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Check">Targeted check slug, or null for selector rules.</param>
/// <param name="Selector">Label selector, or null for single-check rules.</param>
/// <param name="When">Condition.</param>
/// <param name="For">Consecutive matching results to fire.</param>
/// <param name="RecoverAfter">Consecutive non-matching results to resolve.</param>
/// <param name="Severity">Alert severity.</param>
/// <param name="Enabled">Whether it is evaluated.</param>
/// <param name="ManagedBy">Owning front end.</param>
public sealed record RuleDto(
    string Slug,
    string Name,
    string? Check,
    IReadOnlyDictionary<string, string>? Selector,
    RuleWhen When,
    int For,
    int RecoverAfter,
    string Severity,
    bool Enabled,
    string ManagedBy)
{
    /// <summary>Maps a rule; <paramref name="checkSlug"/> is the slug of <see cref="Rule.CheckId"/>.</summary>
    public static RuleDto From(Rule rule, string? checkSlug)
    {
        var when = rule.Condition switch
        {
            ConditionKind.Down => new RuleWhen("down", null, null, null),
            ConditionKind.Above => new RuleWhen(null, rule.Dimension, rule.Threshold, null),
            _ => new RuleWhen(null, rule.Dimension, null, rule.Threshold),
        };

        return new RuleDto(
            rule.Slug,
            rule.Name,
            checkSlug,
            rule.CheckId is null ? rule.Selector : null,
            when,
            rule.For,
            rule.RecoverAfter,
            JsonNamingPolicy.CamelCase.ConvertName(rule.Severity.ToString()),
            rule.Enabled,
            JsonNamingPolicy.CamelCase.ConvertName(rule.ManagedBy.ToString()));
    }
}
