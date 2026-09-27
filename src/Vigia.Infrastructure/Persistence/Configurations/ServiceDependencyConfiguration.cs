using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Services;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ServiceDependency"/>.
/// </summary>
public sealed class ServiceDependencyConfiguration : IEntityTypeConfiguration<ServiceDependency>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceDependency> builder)
    {
        builder.HasKey(d => new { d.ServiceId, d.DependsOnId });
        builder.Property(d => d.Mode).HasConversion<string>().HasMaxLength(10);

        // Deleting a service removes the edges pointing at it too.
        builder.HasOne<Service>().WithMany().HasForeignKey(d => d.DependsOnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.DependsOnId);
    }
}
