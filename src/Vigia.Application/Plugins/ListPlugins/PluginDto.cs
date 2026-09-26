using Vigia.Application.Plugins;

namespace Vigia.Application.Plugins.ListPlugins;

/// <summary>
/// A plugin as exposed by the API.
/// </summary>
/// <param name="Id">Plugin id.</param>
/// <param name="Version">Installed version.</param>
/// <param name="Kind">Plugin kind, for example <c>check</c>.</param>
/// <param name="Label">Display name.</param>
/// <param name="Description">What it does.</param>
/// <param name="DefaultInterval">Default interval for checks, as a duration string.</param>
/// <param name="Dimensions">Dimensions the plugin measures.</param>
/// <param name="Webhooks">Webhooks each check of this plugin receives.</param>
/// <param name="Schema">Config schema.</param>
public sealed record PluginDto(
    string Id,
    string Version,
    string Kind,
    string Label,
    string Description,
    string? DefaultInterval,
    IReadOnlyList<DimensionDto> Dimensions,
    IReadOnlyList<WebhookDto> Webhooks,
    ConfigSchema Schema);
