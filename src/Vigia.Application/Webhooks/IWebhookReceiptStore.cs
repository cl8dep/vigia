namespace Vigia.Application.Webhooks;

/// <summary>
/// Persistence for webhook receipts. Singleton-safe: implementations manage their own scope.
/// </summary>
public interface IWebhookReceiptStore
{
    /// <summary>Last accepted request time, or null if never.</summary>
    Task<DateTimeOffset?> LastReceivedAsync(Guid checkId, string webhook, CancellationToken ct);

    /// <summary>When the check started accepting webhooks (its creation time).</summary>
    Task<DateTimeOffset> ListeningSinceAsync(Guid checkId, CancellationToken ct);

    /// <summary>Records an accepted request.</summary>
    Task RecordAsync(Guid checkId, string webhook, DateTimeOffset at, CancellationToken ct);
}
