using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Health;
using Vigia.Domain.Services;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ServiceHealth"/>.
/// </summary>
public sealed class ServiceHealthConfiguration : IEntityTypeConfiguration<ServiceHealth>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceHealth> builder)
    {
        builder.ToTable("service_health");
        builder.HasKey(h => h.ServiceId);
        builder.Property(h => h.State).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Reason).HasMaxLength(2000);
        builder.Property(h => h.Partitions)
            .HasColumnType("jsonb")
            .HasConversion(new JsonDictionaryConverter<HealthState>(), new DictionaryComparer<HealthState>());
        builder.HasOne<Service>().WithOne().HasForeignKey<ServiceHealth>(h => h.ServiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
