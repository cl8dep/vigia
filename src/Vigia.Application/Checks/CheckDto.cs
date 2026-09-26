using System.Text.Json;
using System.Text.Json.Nodes;
using Vigia.Application.Common;
using Vigia.Application.Placement;
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
/// <param name="Tags">Tags, including read-only system tags (<c>vigia:*</c>). Null value means a flag.</param>
/// <param name="ManagedBy">Owning front end.</param>
/// <param name="CreatedAt">Creation time (UTC).</param>
/// <param name="UpdatedAt">Last update time (UTC).</param>
/// <param name="Webhooks">Webhooks this check receives, as <c>/api/v1/hooks/{slug}/{name}</c>.</param>
/// <param name="WebhookToken">Webhook token; only present in the response that creates it.</param>
/// <param name="Workers">Which workers run the check and the quorum.</param>
/// <param name="Placement">Where the check can run right now.</param>
public sealed record CheckDto(
    Guid Id,
    string Slug,
    string Name,
    string Plugin,
    JsonElement Config,
    string Interval,
    bool Enabled,
    IReadOnlyDictionary<string, string?> Tags,
    string ManagedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Webhooks,
    string? WebhookToken,
    CheckWorkersDto Workers,
    PlacementDto Placement)
{
    /// <summary>Maps an entity, removing secret fields using the plugin schema when the plugin is loaded.</summary>
    /// <param name="check">Entity.</param>
    /// <param name="plugin">Plugin, or null if not installed (then the whole config is hidden).</param>
    /// <param name="placement">Where the check can run.</param>
    /// <param name="webhookToken">Newly issued webhook token to show once, if any.</param>
    public static CheckDto From(Check check, CheckPlugin? plugin, PlacementDto placement, string? webhookToken = null)
    {
        return new CheckDto(
            check.Id,
            check.Slug,
            check.Name,
            check.Plugin,
            RedactSecrets(check.ConfigJson, plugin),
            Duration.Format(check.Interval),
            check.Enabled,
            check.Tags,
            JsonNamingPolicy.CamelCase.ConvertName(check.ManagedBy.ToString()),
            check.CreatedAt,
            check.UpdatedAt,
            plugin?.Webhooks.Keys.Order().ToList() ?? [],
            webhookToken,
            CheckWorkersSpecApplier.ToDto(check),
            placement);
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
