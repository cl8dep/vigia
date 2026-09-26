using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ValidationException = Vigia.Application.Common.Exceptions.ValidationException;
using Vigia.Application.Common.Json;

namespace Vigia.Application.Plugins;

/// <summary>
/// Turns raw JSON config into a validated plugin config instance.
/// </summary>
public static class ConfigBinder
{
    /// <summary>Binds and validates <paramref name="config"/> for <paramref name="plugin"/>.</summary>
    /// <param name="plugin">Target plugin.</param>
    /// <param name="config">Raw config.</param>
    /// <param name="fieldPrefix">Prefix for error paths, for example <c>config</c>.</param>
    /// <exception cref="ValidationException">The config does not match the schema or fails validation.</exception>
    public static BoundConfig Bind(CheckPlugin plugin, JsonElement config, string fieldPrefix)
    {
        if (config.ValueKind is not JsonValueKind.Object)
        {
            throw new ValidationException(fieldPrefix, "Config must be a JSON object.");
        }

        var configType = plugin.ConfigType;
        object value;
        try
        {
            value = config.Deserialize(configType, ConfigJson.Options)
                ?? throw new ValidationException(fieldPrefix, "Config is required.");
        }
        catch (JsonException ex)
        {
            throw new ValidationException(ToFieldPath(fieldPrefix, ex.Path), ex.Message);
        }

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true))
        {
            var errors = results
                .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty), (r, member) => (Field: member, r.ErrorMessage))
                .GroupBy(e => ToFieldPath(fieldPrefix, JsonNamingPolicy.CamelCase.ConvertName(e.Field)))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Invalid value.").ToArray());
            throw new ValidationException(errors);
        }

        return new BoundConfig(value, JsonSerializer.SerializeToElement(value, configType, ConfigJson.Options));
    }

    private static string ToFieldPath(string prefix, string? path)
    {
        var trimmed = path?.TrimStart('$').TrimStart('.');
        return string.IsNullOrEmpty(trimmed) ? prefix : $"{prefix}.{trimmed}";
    }
}
