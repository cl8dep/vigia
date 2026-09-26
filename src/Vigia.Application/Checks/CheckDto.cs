using System.Text.Json;
using System.Text.Json.Nodes;
using Vigia.Application.Common;
using Vigia.Application.Plugins;
using Vigia.Domain.Checks;

namespace Vigia.Application.Checks;

/// <summary>
/// A check as exposed by the API. Secret config fields are never included.
/// </summary>
/// <param name="Id">Internal id.</param>
/// <param name="Slug">Stable identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Plugin">Plugin id.</param>
/// <param name="Config">Config without secret fields.</param>
/// <param name="Interval">Interval as a duration string.</param>
/// <param name="Enabled">Whether the check is scheduled.</param>
/// <param name="Labels">Labels.</param>
/// <param name="ManagedBy">Owning front end.</param>
/// <param name="CreatedAt">Creation time (UTC).</param>
/// <param name="UpdatedAt">Last update time (UTC).</param>
public sealed record CheckDto(
    Guid Id,
    string Slug,
    string Name,
    string Plugin,
    JsonElement Config,
    string Interval,
    bool Enabled,
    IReadOnlyDictionary<string, string> Labels,
    string ManagedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Maps an entity, removing secret fields using the plugin schema when the plugin is loaded.</summary>
    /// <param name="check">Entity.</param>
    /// <param name="plugin">Plugin, or null if not installed (then the whole config is hidden).</param>
    public static CheckDto From(Check check, CheckPlugin? plugin)
    {
        return new CheckDto(
            check.Id,
            check.Slug,
            check.Name,
            check.Plugin,
            RedactSecrets(check.ConfigJson, plugin),
            Duration.Format(check.Interval),
            check.Enabled,
            check.Labels,
            JsonNamingPolicy.CamelCase.ConvertName(check.ManagedBy.ToString()),
            check.CreatedAt,
            check.UpdatedAt);
    }

    private static JsonElement RedactSecrets(string configJson, CheckPlugin? plugin)
    {
        // Without the schema we cannot tell which fields are secret, so nothing is returned.
        if (plugin is null)
        {
            return JsonSerializer.SerializeToElement(new { });
        }

        var node = JsonNode.Parse(configJson)!.AsObject();
        foreach (var field in plugin.Schema.Fields.Where(f => f.Secret))
        {
            node.Remove(field.Name);
        }

        return JsonSerializer.SerializeToElement(node);
    }
}
