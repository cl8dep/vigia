using System.Text.RegularExpressions;
using Vigia.Domain.Common;

namespace Vigia.Domain.Tags;

/// <summary>
/// Syntax rules for tags: <c>key</c> (a flag) or <c>key:value</c>. Same limits as Piro (RFC 0008).
/// </summary>
public static partial class TagRules
{
    /// <summary>Maximum tags on one entity, system tags included.</summary>
    public const int MaxTagsPerEntity = 50;

    /// <summary>Maximum key length.</summary>
    public const int MaxKeyLength = 63;

    /// <summary>Maximum value length.</summary>
    public const int MaxValueLength = 255;

    /// <summary>Validates user-supplied tags. System keys are rejected: the system derives them.</summary>
    /// <exception cref="DomainException">A key or value breaks the rules, or there are too many tags.</exception>
    public static void ValidateUserTags(IReadOnlyDictionary<string, string?> tags)
    {
        if (tags.Count > MaxTagsPerEntity)
        {
            throw new DomainException($"At most {MaxTagsPerEntity} tags per entity.");
        }

        foreach (var (key, value) in tags)
        {
            if (SystemTags.IsReserved(key))
            {
                throw new DomainException($"Tag '{key}' is in the reserved '{SystemTags.Prefix}' namespace; the system sets those.");
            }

            ValidateUserKey(key);
            ValidateValue(key, value);
        }
    }

    /// <summary>Validates a key used in a selector: a user key or a known system tag.</summary>
    /// <exception cref="DomainException">The key is invalid.</exception>
    public static void ValidateSelectorKey(string key)
    {
        if (SystemTags.IsReserved(key))
        {
            if (!SystemTags.All.Contains(key))
            {
                throw new DomainException($"Unknown system tag '{key}'. Known: {string.Join(", ", SystemTags.All)}.");
            }

            return;
        }

        ValidateUserKey(key);
    }

    /// <summary>Validates a value.</summary>
    /// <exception cref="DomainException">The value is too long.</exception>
    public static void ValidateValue(string key, string? value)
    {
        if (value is { Length: > MaxValueLength })
        {
            throw new DomainException($"The value of tag '{key}' exceeds {MaxValueLength} characters.");
        }
    }

    private static void ValidateUserKey(string key)
    {
        if (key.Length is 0 or > MaxKeyLength || !UserKeyPattern().IsMatch(key))
        {
            throw new DomainException($"Tag key '{key}' must start with a lowercase letter and contain only lowercase letters, digits, '-' and '_' (max {MaxKeyLength}).");
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_-]*$")]
    private static partial Regex UserKeyPattern();
}
