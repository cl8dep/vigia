using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Vigia.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores a string-keyed dictionary as a JSON object (tags, measurements).
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public sealed class JsonDictionaryConverter<TValue> : ValueConverter<Dictionary<string, TValue>, string>
{
    /// <summary>Creates the converter.</summary>
    public JsonDictionaryConverter()
        : base(v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
               v => JsonSerializer.Deserialize<Dictionary<string, TValue>>(v, JsonSerializerOptions.Default) ?? new())
    {
    }
}
