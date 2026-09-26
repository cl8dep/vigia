using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Webhooks;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Webhooks;

/// <summary>
/// Webhook receipts in Postgres. Recording is a single upsert, so concurrent requests never race.
/// </summary>
public sealed class DbWebhookReceiptStore(IServiceScopeFactory scopes) : IWebhookReceiptStore
{
    /// <inheritdoc />
    public async Task<DateTimeOffset?> LastReceivedAsync(Guid checkId, string webhook, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.WebhookReceipts
            .Where(r => r.CheckId == checkId && r.Webhook == webhook)
            .Select(r => (DateTimeOffset?)r.LastReceivedAt)
            .SingleOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset> ListeningSinceAsync(Guid checkId, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Checks.Where(c => c.Id == checkId).Select(c => c.CreatedAt).SingleAsync(ct);
    }

    /// <inheritdoc />
    public async Task RecordAsync(Guid checkId, string webhook, DateTimeOffset at, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO webhook_receipts (check_id, webhook, last_received_at, count)
            VALUES ({0}, {1}, {2}, 1)
            ON CONFLICT (check_id, webhook) DO UPDATE
            SET last_received_at = greatest(webhook_receipts.last_received_at, excluded.last_received_at),
                count = webhook_receipts.count + 1
            """,
            [checkId, webhook, at],
            ct);
    }
}
