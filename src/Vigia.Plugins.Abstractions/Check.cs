namespace Vigia.Plugins;

/// <summary>
/// Base class for check plugins with a typed config record.
/// </summary>
/// <typeparam name="TConfig">Config record. Its attributes define the schema shown in UI, YAML and Terraform.</typeparam>
public abstract class Check<TConfig> : ICheck where TConfig : class
{
    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract CheckManifest Manifest { get; }

    /// <summary>Runs one probe with a typed config.</summary>
    /// <param name="config">Validated config.</param>
    /// <param name="ctx">Services provided by the host.</param>
    /// <param name="ct">Cancelled when the probe exceeds its timeout or the host shuts down.</param>
    public abstract Task<ProbeResult> ProbeAsync(TConfig config, ICheckContext ctx, CancellationToken ct);

    Task<ProbeResult> ICheck.ProbeAsync(object config, ICheckContext ctx, CancellationToken ct)
    {
        if (config is not TConfig typed)
        {
            throw new ArgumentException($"Check '{Id}' expects {typeof(TConfig).Name}, got {config.GetType().Name}.");
        }

        return ProbeAsync(typed, ctx, ct);
    }
}
