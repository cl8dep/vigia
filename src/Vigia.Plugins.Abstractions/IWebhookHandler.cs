namespace Vigia.Plugins;

/// <summary>
/// Handles requests to one webhook. Declared in the manifest (<c>webhooks[].class</c>); the host creates one instance at load
/// and authenticates every request before calling it.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>Processes one request. Must not throw for malformed input; return <see cref="WebhookOutcome.Rejected"/>.</summary>
    Task<WebhookOutcome> HandleAsync(WebhookRequest request, IWebhookContext context, CancellationToken ct);
}
