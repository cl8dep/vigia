namespace Vigia.Plugins;

/// <summary>
/// When the current check last received each of its webhooks. Available to probes of plugins that declare webhooks;
/// lives on the control plane, so such checks run there.
/// </summary>
public interface IWebhookReceipts
{
    /// <summary>Time of the last accepted request to <paramref name="webhook"/> for this check, or null if never.</summary>
    /// <exception cref="InvalidOperationException">The webhook is not declared in the plugin manifest.</exception>
    Task<DateTimeOffset?> LastReceivedAsync(string webhook, CancellationToken ct);
}
