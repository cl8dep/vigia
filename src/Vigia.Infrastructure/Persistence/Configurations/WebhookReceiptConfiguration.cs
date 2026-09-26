using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Checks;
using Vigia.Domain.Webhooks;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="WebhookReceipt"/>.
/// </summary>
public sealed class WebhookReceiptConfiguration : IEntityTypeConfiguration<WebhookReceipt>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WebhookReceipt> builder)
    {
        // Raw SQL in DbWebhookReceiptStore depends on this name and key.
        builder.ToTable("webhook_receipts");
        builder.HasKey(r => new { r.CheckId, r.Webhook });
        builder.Property(r => r.Webhook).HasMaxLength(63);
        builder.HasOne<Check>().WithMany().HasForeignKey(r => r.CheckId).OnDelete(DeleteBehavior.Cascade);
    }
}
