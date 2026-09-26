using System.Text.Json;
using System.Text.Json.Nodes;
using Vigia.Application.Common.Exceptions;
using Vigia.Domain.Common;
using Vigia.Domain.Tags;

namespace Vigia.Application.Common.Json;

/// <summary>
/// JSON shape of a <see cref="TagSelector"/>: each key maps to a string (one value), an array (any of the values)
/// or <c>null</c> (the key must be present). For example <c>{ "region": ["eu", "us"], "network": "vpc", "critical": null }</c>.
/// </summary>
public static class TagSelectorJson
{
    /// <summary>Parses client input.</summary>
    /// <param name="json">Selector as sent by the client.</param>
    /// <param name="field">Field name for errors.</param>
    /// <exception cref="ValidationException">A value has the wrong shape or a tag is invalid.</exception>
    public static TagSelector Parse(IReadOnlyDictionary<string, JsonElement> json, string field)
    {
        var terms = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (key, value) in json)
        {
            terms[key] = value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => [],
                JsonValueKind.String => [value.GetString()!],
                JsonValueKind.Array when value.EnumerateArray().All(v => v.ValueKind == JsonValueKind.String) =>
                    [.. value.EnumerateArray().Select(v => v.GetString()!)],
                _ => throw new ValidationException($"{field}.{key}", "Use a string, an array of strings, or null for 'key present'."),
            };
        }

        try
        {
            return TagSelector.Create(terms);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(field, ex.Message);
        }
    }

    /// <summary>Writes a selector in the same shape clients send.</summary>
    public static IReadOnlyDictionary<string, JsonNode?> Write(TagSelector selector)
    {
        return selector.Terms.ToDictionary(
            t => t.Key,
            t => t.Value.Count switch
            {
                0 => null,
                1 => (JsonNode?)JsonValue.Create(t.Value[0]),
                _ => new JsonArray([.. t.Value.Select(v => (JsonNode?)JsonValue.Create(v))]),
            });
    }
}
