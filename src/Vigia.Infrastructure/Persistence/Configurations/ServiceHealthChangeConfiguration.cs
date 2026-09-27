using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Health;
using Vigia.Domain.Services;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ServiceHealthChange"/>.
/// </summary>
public sealed class ServiceHealthChangeConfiguration : IEntityTypeConfiguration<ServiceHealthChange>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceHealthChange> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.From).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.To).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Reason).HasMaxLength(2000);
        builder.HasIndex(c => new { c.ServiceId, c.At }).IsDescending(false, true);
        builder.HasOne<Service>().WithMany().HasForeignKey(c => c.ServiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
