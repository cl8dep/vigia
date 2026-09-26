namespace Vigia.Application.Plugins;

/// <summary>
/// A plugin folder the host could not load.
/// </summary>
/// <param name="Path">Plugin folder.</param>
/// <param name="Reason">Why it was rejected.</param>
public sealed record PluginLoadFailure(string Path, string Reason);
