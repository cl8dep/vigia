using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vigia.Domain.Tags;

namespace Vigia.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores a <see cref="TagSelector"/> as a JSON object of key to value array (<c>[]</c> means "key present").
/// </summary>
public sealed class TagSelectorConverter : ValueConverter<TagSelector, string>
{
    /// <summary>Creates the converter.</summary>
    public TagSelectorConverter()
        : base(v => Serialize(v), v => Deserialize(v))
    {
    }

    /// <summary>Canonical JSON for a selector; also used to compare selectors.</summary>
    public static string Serialize(TagSelector selector)
    {
        var ordered = selector.Terms.OrderBy(t => t.Key, StringComparer.Ordinal).ToDictionary(t => t.Key, t => t.Value.Order(StringComparer.Ordinal).ToList());
        return JsonSerializer.Serialize(ordered, JsonSerializerOptions.Default);
    }

    private static TagSelector Deserialize(string json)
    {
        var terms = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonSerializerOptions.Default) ?? [];
        return TagSelector.Create(terms.ToDictionary(t => t.Key, t => (IReadOnlyList<string>)t.Value));
    }
}
