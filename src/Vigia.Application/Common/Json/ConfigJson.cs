using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vigia.Application.Common.Json;

/// <summary>
/// JSON settings shared by everything that reads or writes plugin config.
/// </summary>
public static class ConfigJson
{
    /// <summary>
    /// camelCase names, durations as strings, and unknown fields rejected so typos in YAML or Terraform fail loudly.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new DurationJsonConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
