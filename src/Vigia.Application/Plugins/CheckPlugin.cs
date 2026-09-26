using Vigia.Application.Plugins.Manifest;
using Vigia.Plugins;

namespace Vigia.Application.Plugins;

/// <summary>
/// A loaded check plugin: its manifest plus the instances and schema the host built from it.
/// </summary>
/// <param name="Manifest">Parsed <c>plugin.json</c>.</param>
/// <param name="Check">Instance of <c>check.class</c>.</param>
/// <param name="ConfigType">Type named by <c>check.config</c>.</param>
/// <param name="Schema">Config schema built from <paramref name="ConfigType"/>.</param>
/// <param name="Webhooks">Instances of the declared webhook classes, by webhook name.</param>
public sealed record CheckPlugin(
    PluginManifest Manifest,
    ICheck Check,
    Type ConfigType,
    ConfigSchema Schema,
    IReadOnlyDictionary<string, IWebhookHandler> Webhooks)
{
    /// <summary>Plugin id.</summary>
    public string Id
    {
        get { return Manifest.Id; }
    }

    /// <summary>Plugin version.</summary>
    public string Version
    {
        get { return Manifest.Version; }
    }

    /// <summary>Interval for checks that do not set one.</summary>
    public TimeSpan DefaultInterval
    {
        get { return Manifest.Check.DefaultInterval ?? TimeSpan.FromMinutes(1); }
    }

    /// <summary>Declared dimensions.</summary>
    public IReadOnlyList<DimensionDeclaration> Dimensions
    {
        get { return Manifest.Check.Dimensions ?? []; }
    }
}
