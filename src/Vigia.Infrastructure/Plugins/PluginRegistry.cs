using Vigia.Application.Plugins;

namespace Vigia.Infrastructure.Plugins;

/// <summary>
/// Immutable set of plugins produced by <see cref="PluginLoader"/>.
/// </summary>
public sealed class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, CheckPlugin> _checks;

    /// <summary>Creates the registry.</summary>
    public PluginRegistry(IEnumerable<CheckPlugin> checks, IEnumerable<PluginLoadFailure> failures)
    {
        _checks = checks.ToDictionary(c => c.Id, StringComparer.Ordinal);
        Failures = failures.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<CheckPlugin> Checks
    {
        get { return _checks.Values; }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<PluginLoadFailure> Failures { get; }

    /// <inheritdoc />
    public bool TryGetCheck(string id, out CheckPlugin plugin)
    {
        return _checks.TryGetValue(id, out plugin!);
    }
}
