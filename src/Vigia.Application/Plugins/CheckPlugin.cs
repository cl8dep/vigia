using Vigia.Plugins;

namespace Vigia.Application.Plugins;

/// <summary>
/// A loaded check plugin with its precomputed config schema.
/// </summary>
/// <param name="Id">Plugin id from <c>plugin.json</c>.</param>
/// <param name="Version">Plugin version from <c>plugin.json</c>.</param>
/// <param name="Check">Plugin instance.</param>
/// <param name="Schema">Config schema built at load time.</param>
public sealed record CheckPlugin(string Id, string Version, ICheck Check, ConfigSchema Schema);
