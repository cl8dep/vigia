namespace Vigia.Domain.Tags;

/// <summary>
/// What a system tag's value may be.
/// </summary>
public enum TagValueKind
{
    /// <summary>Key only: presence is the fact. The value must be null.</summary>
    Flag,

    /// <summary>Any string.</summary>
    Value,

    /// <summary>One of a closed list of values.</summary>
    Vocabulary,
}
