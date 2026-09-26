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

    /// <summary>
    /// Validates tags written by a user on an entity: user keys, plus assignable system tags that apply to the entity
    /// with a value of the right kind. Reconciled and computed system tags are rejected: the system owns them.
    /// </summary>
    /// <exception cref="DomainException">A key or value breaks the rules, or there are too many tags.</exception>
    public static void ValidateWrittenTags(IReadOnlyDictionary<string, string?> tags, TaggedEntity entity)
    {
        if (tags.Count > MaxTagsPerEntity)
        {
            throw new DomainException($"At most {MaxTagsPerEntity} tags per entity.");
        }

        foreach (var (key, value) in tags)
        {
            if (SystemTags.IsReserved(key))
            {
                ValidateAssignable(key, value, entity);
                continue;
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
            if (SystemTags.Find(key) is null)
            {
                throw new DomainException($"Unknown system tag '{key}'. Known: {string.Join(", ", SystemTags.All.Select(d => d.Key))}.");
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

    private static void ValidateAssignable(string key, string? value, TaggedEntity entity)
    {
        var definition = SystemTags.Find(key)
            ?? throw new DomainException($"Unknown system tag '{key}'. The '{SystemTags.Prefix}' namespace is reserved.");

        if (definition.Assignment != TagAssignment.Assignable)
        {
            throw new DomainException($"Tag '{key}' is set by the system and cannot be written.");
        }

        if (!definition.AppliesTo.Contains(entity))
        {
            throw new DomainException($"Tag '{key}' does not apply to {entity.ToString().ToLowerInvariant()}s.");
        }

        switch (definition.ValueKind)
        {
            case TagValueKind.Flag when value is not null:
                throw new DomainException($"Tag '{key}' is a flag; give it no value (null).");
            case TagValueKind.Value when value is null:
                throw new DomainException($"Tag '{key}' needs a value.");
            case TagValueKind.Vocabulary when value is null || !(definition.AllowedValues ?? []).Contains(value):
                throw new DomainException($"Tag '{key}' must be one of: {string.Join(", ", definition.AllowedValues ?? [])}.");
        }

        ValidateValue(key, value);
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
