using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Checks;
using Vigia.Domain.Common;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Vigia.Domain.Checks.Check"/>.
/// </summary>
public sealed class CheckConfiguration : IEntityTypeConfiguration<Check>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Check> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Plugin).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PluginVersion).HasMaxLength(50).IsRequired();
        builder.Property(c => c.ConfigJson).HasColumnType("jsonb").IsRequired();
        builder.Property(c => c.Labels)
            .HasColumnType("jsonb")
            .HasConversion(new JsonDictionaryConverter<string>(), new DictionaryComparer<string>());
        builder.Property(c => c.ManagedBy).HasConversion<string>().HasMaxLength(20);
    }
}
