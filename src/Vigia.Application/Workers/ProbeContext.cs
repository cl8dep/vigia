using Vigia.Application.Plugins;
using Vigia.Application.Webhooks;
using Vigia.Plugins;

namespace Vigia.Application.Workers;

/// <summary>
/// <see cref="ICheckContext"/> for one probe of one check. Adds the capability-gated services the plugin declared.
/// </summary>
public sealed class ProbeContext(ICheckContext host, Guid checkId, CheckPlugin plugin, IWebhookReceiptStore? receipts) : ICheckContext
{
    /// <inheritdoc />
    public TimeProvider Time
    {
        get { return host.Time; }
    }

    /// <inheritdoc />
    public HttpClient CreateHttpClient()
    {
        return host.CreateHttpClient();
    }

    /// <inheritdoc />
    public T GetRequiredService<T>() where T : class
    {
        if (typeof(T) != typeof(IWebhookReceipts))
        {
            throw new InvalidOperationException($"Plugin '{plugin.Id}' requested {typeof(T).Name}, which is not available to checks.");
        }

        if (plugin.Webhooks.Count == 0)
        {
            throw new InvalidOperationException($"Plugin '{plugin.Id}' requested {nameof(IWebhookReceipts)} but declares no webhooks.");
        }

        if (receipts is null)
        {
            throw new InvalidOperationException($"Plugin '{plugin.Id}' reads webhook receipts, which only exist on the control plane.");
        }

        return (T)(object)new CheckWebhookReceipts(checkId, plugin, receipts);
    }
}
