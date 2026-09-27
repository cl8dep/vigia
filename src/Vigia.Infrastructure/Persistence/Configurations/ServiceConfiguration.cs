using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vigia.Domain.Common;
using Vigia.Domain.Services;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Service"/> and its dependencies.
/// </summary>
public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Tags).HasColumnType("jsonb").HasConversion(new JsonDictionaryConverter<string?>(), new DictionaryComparer<string?>());
        // Nullable selector: EF never passes null to the converter.
        builder.Property(s => s.Checks).HasColumnType("jsonb").HasConversion((ValueConverter)new TagSelectorConverter(), new TagSelectorComparer());
        builder.Property(s => s.PartitionBy).HasMaxLength(63);
        builder.Property(s => s.ManagedBy).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(s => s.Dependencies).WithOne().HasForeignKey(d => d.ServiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Dependencies).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
