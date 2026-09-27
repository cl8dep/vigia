using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Common;
using Vigia.Domain.Services;
using Vigia.Domain.StatusPages;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="StatusPage"/> and its components.
/// </summary>
public sealed class StatusPageConfiguration : IEntityTypeConfiguration<StatusPage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StatusPage> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Tags).HasColumnType("jsonb").HasConversion(new JsonDictionaryConverter<string?>(), new DictionaryComparer<string?>());
        builder.Property(p => p.ManagedBy).HasConversion<string>().HasMaxLength(20);

        builder.OwnsMany(p => p.Components, component =>
        {
            component.ToTable("status_page_components");
            component.WithOwner().HasForeignKey(c => c.StatusPageId);
            component.HasKey(c => new { c.StatusPageId, c.ServiceId });
            component.Property(c => c.Name).HasMaxLength(200).IsRequired();
            component.Property(c => c.Group).HasMaxLength(200);

            // Deleting a service removes it from every page.
            component.HasOne<Service>().WithMany().HasForeignKey(c => c.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Navigation(p => p.Components).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
