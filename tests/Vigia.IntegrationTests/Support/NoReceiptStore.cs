using Vigia.Application.Webhooks;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Receipt store that never has receipts.
/// </summary>
public sealed class NoReceiptStore : IWebhookReceiptStore
{
    /// <inheritdoc />
    public Task<DateTimeOffset?> LastReceivedAsync(Guid checkId, string webhook, CancellationToken ct)
    {
        return Task.FromResult<DateTimeOffset?>(null);
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> ListeningSinceAsync(Guid checkId, CancellationToken ct)
    {
        return Task.FromResult(DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public Task RecordAsync(Guid checkId, string webhook, DateTimeOffset at, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
