namespace Vigia.Plugins;

/// <summary>
/// A check plugin: probes a target and reports whether it is up, down, or could not be probed.
/// </summary>
/// <remarks>
/// Declared in the plugin's <c>plugin.json</c> (<c>check.class</c>), which the host reads to find and instantiate it.
/// Identity, labels, dimensions and webhooks live in the manifest, not in code.
/// Implement <see cref="Check{TConfig}"/> instead of this interface directly; it gives a typed config.
/// </remarks>
public interface ICheck
{
    /// <summary>Runs one probe.</summary>
    /// <param name="config">Config instance of the type declared in <c>check.config</c>.</param>
    /// <param name="ctx">Services provided by the host.</param>
    /// <param name="ct">Cancelled when the probe exceeds its timeout or the host shuts down.</param>
    Task<ProbeResult> ProbeAsync(object config, ICheckContext ctx, CancellationToken ct);
}
