using Mediator;

namespace Vigia.Application.Plugins.ListPlugins;

/// <summary>
/// Lists installed plugins with their config schemas.
/// </summary>
public sealed record ListPluginsQuery : IQuery<IReadOnlyList<PluginDto>>;
