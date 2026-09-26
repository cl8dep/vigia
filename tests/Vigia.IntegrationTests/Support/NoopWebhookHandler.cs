using Vigia.Plugins;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Webhook handler that accepts everything and does nothing.
/// </summary>
public sealed class NoopWebhookHandler : IWebhookHandler
{
    /// <inheritdoc />
    public Task<WebhookOutcome> HandleAsync(WebhookRequest request, IWebhookContext context, CancellationToken ct)
    {
        return Task.FromResult(WebhookOutcome.Accepted);
    }
}
