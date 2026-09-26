namespace Vigia.Application.Plugins;

/// <summary>
/// Plugins loaded at startup.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>All loaded check plugins.</summary>
    IReadOnlyCollection<CheckPlugin> Checks { get; }

    /// <summary>Plugins that failed to load, with the reason. Shown in the UI; never fatal.</summary>
    IReadOnlyCollection<PluginLoadFailure> Failures { get; }

    /// <summary>Finds a check plugin by id.</summary>
    bool TryGetCheck(string id, out CheckPlugin plugin);
}
