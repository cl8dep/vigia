namespace Vigia.Plugins;

/// <summary>
/// A check plugin: probes a target and reports whether it is up, down, or could not be probed.
/// </summary>
/// <remarks>
/// Implement <see cref="Check{TConfig}"/> instead of this interface directly; it gives a typed config.
/// The host uses this non-generic interface because config types are only known at runtime.
/// </remarks>
public interface ICheck
{
    /// <summary>Stable plugin id, for example <c>vigia.check.http</c>. Must match <c>plugin.json</c>.</summary>
    string Id { get; }

    /// <summary>Describes the check to the host: label, config type, dimensions it measures.</summary>
    CheckManifest Manifest { get; }

    /// <summary>Runs one probe.</summary>
    /// <param name="config">Config instance of type <see cref="CheckManifest.ConfigType"/>.</param>
    /// <param name="ctx">Services provided by the host.</param>
    /// <param name="ct">Cancelled when the probe exceeds its timeout or the host shuts down.</param>
    Task<ProbeResult> ProbeAsync(object config, ICheckContext ctx, CancellationToken ct);
}
