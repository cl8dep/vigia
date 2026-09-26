using System.Text.Json;
using Mediator;
using Vigia.Application.Common;

namespace Vigia.Application.Plugins.ListPlugins;

/// <summary>
/// Handles <see cref="ListPluginsQuery"/>.
/// </summary>
public sealed class ListPluginsHandler(IPluginRegistry registry) : IQueryHandler<ListPluginsQuery, IReadOnlyList<PluginDto>>
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<PluginDto>> Handle(ListPluginsQuery query, CancellationToken ct)
    {
        IReadOnlyList<PluginDto> plugins = registry.Checks
            .OrderBy(p => p.Id)
            .Select(ToDto)
            .ToList();
        return ValueTask.FromResult(plugins);
    }

    private static PluginDto ToDto(CheckPlugin plugin)
    {
        var dimensions = plugin.Dimensions
            .Select(d => new DimensionDto(d.Name, JsonNamingPolicy.CamelCase.ConvertName(d.Direction.ToString()), d.Unit))
            .ToList();

        return new PluginDto(
            plugin.Id,
            plugin.Version,
            "check",
            plugin.Manifest.Label,
            plugin.Manifest.Description,
            Duration.Format(plugin.DefaultInterval),
            dimensions,
            plugin.Manifest.Webhooks?.Select(w => new WebhookDto(w.Name, w.Description)).ToList() ?? [],
            plugin.Schema);
    }
}
