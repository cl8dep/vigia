using Vigia.Domain.Common;
using Vigia.Domain.Tags;

namespace Vigia.Domain.Checks;

/// <summary>
/// A configured instance of a check plugin: what to probe, how often, and with which config.
/// </summary>
/// <remarks>
/// Checks are standalone. Services include them through label selectors, never the other way around.
/// </remarks>
public sealed class Check : Entity
{
    /// <summary>Shortest allowed interval between probes.</summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);

    private Check()
    {
    }

    /// <summary>Creates a check.</summary>
    /// <param name="slug">Stable identity.</param>
    /// <param name="name">Display name.</param>
    /// <param name="plugin">Plugin id, for example <c>vigia.check.http</c>.</param>
    /// <param name="pluginVersion">Plugin version the config was validated against.</param>
    /// <param name="configJson">Plugin config as JSON, already validated against the plugin schema.</param>
    /// <param name="interval">Time between probes.</param>
    /// <param name="managedBy">Front end that owns the check.</param>
    /// <exception cref="DomainException">An argument breaks an invariant.</exception>
    public Check(string slug, string name, string plugin, string pluginVersion, string configJson, TimeSpan interval, ManagedBy managedBy)
        : base(slug, name, managedBy)
    {
        if (string.IsNullOrWhiteSpace(plugin))
        {
            throw new DomainException("Plugin is required.");
        }

        Plugin = plugin;
        SetSystemTag(SystemTags.Plugin, plugin);
        Reconfigure(pluginVersion, configJson, interval);
        Enabled = true;
    }

    /// <summary>Id of the check plugin that runs this check.</summary>
    public string Plugin { get; private set; } = string.Empty;

    /// <summary>
    /// Plugin version the config was last validated against. Tells which config migrations to apply
    /// when a newer plugin version is installed.
    /// </summary>
    public string PluginVersion { get; private set; } = string.Empty;

    /// <summary>Plugin config as JSON. Validated by the plugin schema before it reaches the entity.</summary>
    public string ConfigJson { get; private set; } = "{}";

    /// <summary>Time between probes.</summary>
    public TimeSpan Interval { get; private set; }

    /// <summary>
    /// SHA-256 of the token that authenticates this check's webhooks, or null when the plugin declares none.
    /// The token itself is only shown once.
    /// </summary>
    public string? WebhookTokenHash { get; private set; }

    /// <summary>Tags a worker must have to run this check. <see cref="TagSelector.Any"/> means every worker.</summary>
    public TagSelector WorkerSelector { get; private set; } = TagSelector.Any;

    /// <summary>Workers that must agree before a rule fires, or null for the global default.</summary>
    public Quorum? Quorum { get; private set; }

    /// <summary>Disabled checks are kept but not scheduled.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Replaces config and interval.</summary>
    /// <param name="pluginVersion">Plugin version the config was validated against.</param>
    /// <param name="configJson">Validated, normalized config.</param>
    /// <param name="interval">Time between probes.</param>
    /// <exception cref="DomainException">The interval is shorter than <see cref="MinInterval"/>.</exception>
    public void Reconfigure(string pluginVersion, string configJson, TimeSpan interval)
    {
        if (interval < MinInterval)
        {
            throw new DomainException($"Interval must be at least {MinInterval.TotalSeconds}s.");
        }

        PluginVersion = pluginVersion;
        ConfigJson = configJson;
        Interval = interval;
    }

    /// <summary>Sets or replaces the webhook token hash, invalidating the previous token.</summary>
    public void SetWebhookTokenHash(string hash)
    {
        WebhookTokenHash = hash;
    }

    /// <summary>Sets which workers may run the check and how many must agree.</summary>
    public void PlaceOn(TagSelector workerSelector, Quorum? quorum)
    {
        WorkerSelector = workerSelector;
        Quorum = quorum;
    }

    /// <summary>Stops scheduling this check.</summary>
    public void Disable()
    {
        Enabled = false;
    }

    /// <summary>Resumes scheduling this check.</summary>
    public void Enable()
    {
        Enabled = true;
    }
}
