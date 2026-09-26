using Vigia.Plugins;

namespace Vigia.Check.Heartbeat;

/// <summary>
/// Handles the <c>ping</c> webhook declared in plugin.json: the job reports it ran. Recording an up result right away lets the check recover
/// without waiting for the next evaluation; the host records the receipt the probe reads.
/// </summary>
public sealed class PingHandler : IWebhookHandler
{
    /// <inheritdoc />
    public async Task<WebhookOutcome> HandleAsync(WebhookRequest request, IWebhookContext context, CancellationToken ct)
    {
        await context.RecordResultAsync(ProbeResult.Up(new Measurement(HeartbeatCheck.SinceLastPing, 0)), ct);
        return WebhookOutcome.Accepted;
    }
}
