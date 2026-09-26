namespace Vigia.Application.Rules;

/// <summary>
/// Everything that defines a rule except its slug. Shared by create and update (full replace).
/// </summary>
public record RuleSpec
{
    /// <summary>Display name. Defaults to the slug.</summary>
    public string? Name { get; init; }

    /// <summary>Slug of the one check to target. Mutually exclusive with <see cref="Selector"/>.</summary>
    public string? Check { get; init; }

    /// <summary>Labels a check must all have to be targeted; <c>plugin</c> matches the plugin id.</summary>
    public IReadOnlyDictionary<string, string>? Selector { get; init; }

    /// <summary>Condition.</summary>
    public RuleWhen? When { get; init; }

    /// <summary>Consecutive matching results to fire. Default 1.</summary>
    public int? For { get; init; }

    /// <summary>Consecutive non-matching results to resolve. Default 1.</summary>
    public int? RecoverAfter { get; init; }

    /// <summary><c>info</c>, <c>warning</c> or <c>critical</c>. Default <c>warning</c>.</summary>
    public string? Severity { get; init; }

    /// <summary>Default true.</summary>
    public bool? Enabled { get; init; }
}
