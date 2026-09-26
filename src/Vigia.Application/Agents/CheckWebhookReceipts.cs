using Vigia.Application.Plugins;
using Vigia.Application.Webhooks;
using Vigia.Plugins;

namespace Vigia.Application.Agents;

/// <summary>
/// <see cref="IWebhookReceipts"/> bound to one check. Only answers for webhooks the plugin declares.
/// </summary>
public sealed class CheckWebhookReceipts(Guid checkId, CheckPlugin plugin, IWebhookReceiptStore store) : IWebhookReceipts
{
    /// <inheritdoc />
    public Task<DateTimeOffset?> LastReceivedAsync(string webhook, CancellationToken ct)
    {
        if (!plugin.Webhooks.ContainsKey(webhook))
        {
            throw new InvalidOperationException($"Plugin '{plugin.Id}' does not declare webhook '{webhook}'.");
        }

        return store.LastReceivedAsync(checkId, webhook, ct);
    }
}
