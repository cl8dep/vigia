namespace Vigia.Domain.Webhooks;

/// <summary>
/// Last accepted request to one webhook of one check. Kept apart from results so a probe reading it
/// never mistakes its own results for received webhooks. Updated by an atomic upsert in the store.
/// </summary>
public sealed class WebhookReceipt
{
    private WebhookReceipt()
    {
    }

    /// <summary>Creates the first receipt.</summary>
    public WebhookReceipt(Guid checkId, string webhook, DateTimeOffset receivedAt)
    {
        CheckId = checkId;
        Webhook = webhook;
        LastReceivedAt = receivedAt;
        Count = 1;
    }

    /// <summary>Check the webhook belongs to.</summary>
    public Guid CheckId { get; private set; }

    /// <summary>Webhook name as declared by the plugin.</summary>
    public string Webhook { get; private set; } = string.Empty;

    /// <summary>When the last accepted request arrived (UTC).</summary>
    public DateTimeOffset LastReceivedAt { get; private set; }

    /// <summary>Accepted requests so far.</summary>
    public long Count { get; private set; }
}
