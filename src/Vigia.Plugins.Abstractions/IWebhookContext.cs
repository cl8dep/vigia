namespace Vigia.Plugins;

/// <summary>
/// What a webhook handler can do for the check the request targets.
/// </summary>
public interface IWebhookContext
{
    /// <summary>The target check's config, of the plugin's config type.</summary>
    object Config { get; }

    /// <summary>Clock to use instead of <see cref="DateTime.UtcNow"/>.</summary>
    TimeProvider Time { get; }

    /// <summary>Records a result for the check right away, without waiting for the next probe.</summary>
    Task RecordResultAsync(ProbeResult result, CancellationToken ct);
}
