using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vigia.Application.Common.Json;

/// <summary>
/// Reads and writes <see cref="TimeSpan"/> as a <see cref="Duration"/> string (<c>30s</c>).
/// </summary>
public sealed class DurationJsonConverter : JsonConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (!Duration.TryParse(raw, out var value))
        {
            throw new JsonException($"'{raw}' is not a duration. Use values like 500ms, 30s, 5m, 1h.");
        }

        return value;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Duration.Format(value));
    }
}
