using Vigia.Domain.Common;

namespace Vigia.Domain.Tags;

/// <summary>
/// Selects entities by tags: AND across keys, OR within a key's values. A key with no values only requires the key
/// to be present (any value, or a flag). An empty selector matches everything.
/// </summary>
/// <remarks>
/// <c>{ region: [eu, us], network: [vpc] }</c> = (region is eu or us) and network is vpc.
/// <c>{ critical: [] }</c> = has the <c>critical</c> tag.
/// </remarks>
public sealed class TagSelector
{
    private TagSelector(IReadOnlyDictionary<string, IReadOnlyList<string>> terms)
    {
        Terms = terms;
    }

    /// <summary>Selector that matches everything.</summary>
    public static TagSelector Any { get; } = new(new Dictionary<string, IReadOnlyList<string>>());

    /// <summary>Key to accepted values; an empty list means "key present".</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Terms { get; }

    /// <summary>Whether the selector has no terms.</summary>
    public bool IsEmpty
    {
        get { return Terms.Count == 0; }
    }

    /// <summary>Creates a validated selector.</summary>
    /// <exception cref="DomainException">A key or value is invalid.</exception>
    public static TagSelector Create(IReadOnlyDictionary<string, IReadOnlyList<string>> terms)
    {
        foreach (var (key, values) in terms)
        {
            TagRules.ValidateSelectorKey(key);
            foreach (var value in values)
            {
                TagRules.ValidateValue(key, value);
            }
        }

        return new TagSelector(terms.ToDictionary(t => t.Key, t => (IReadOnlyList<string>)t.Value.Distinct().ToList(), StringComparer.Ordinal));
    }

    /// <summary>Whether an entity with <paramref name="tags"/> is selected.</summary>
    public bool Matches(IReadOnlyDictionary<string, string?> tags)
    {
        foreach (var (key, values) in Terms)
        {
            if (!tags.TryGetValue(key, out var value))
            {
                return false;
            }

            if (values.Count > 0 && (value is null || !values.Contains(value)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The single value required for <paramref name="key"/>, when exactly one is.</summary>
    public string? SingleValue(string key)
    {
        return Terms.TryGetValue(key, out var values) && values.Count == 1 ? values[0] : null;
    }
}
