namespace Vigia.Plugins;

/// <summary>
/// Base class for check plugins with a typed config record.
/// </summary>
/// <typeparam name="TConfig">Config record; must match <c>check.config</c> in the manifest. Its attributes define the schema.</typeparam>
public abstract class Check<TConfig> : ICheck where TConfig : class
{
    /// <summary>Runs one probe with a typed config.</summary>
    /// <param name="config">Validated config.</param>
    /// <param name="ctx">Services provided by the host.</param>
    /// <param name="ct">Cancelled when the probe exceeds its timeout or the host shuts down.</param>
    public abstract Task<ProbeResult> ProbeAsync(TConfig config, ICheckContext ctx, CancellationToken ct);

    Task<ProbeResult> ICheck.ProbeAsync(object config, ICheckContext ctx, CancellationToken ct)
    {
        if (config is not TConfig typed)
        {
            throw new ArgumentException($"{GetType().Name} expects {typeof(TConfig).Name}, got {config.GetType().Name}.");
        }

        return ProbeAsync(typed, ctx, ct);
    }
}
