using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Checks;
using Vigia.Domain.Common;
using Vigia.Domain.Rules;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Rule"/>.
/// </summary>
public sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.HasIndex(r => r.Slug).IsUnique();
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Tags).HasColumnType("jsonb").HasConversion(new JsonDictionaryConverter<string?>(), new DictionaryComparer<string?>());
        builder.Property(r => r.Selector).HasColumnType("jsonb").HasConversion(new TagSelectorConverter(), new TagSelectorComparer());
        builder.Property(r => r.Condition).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.Dimension).HasMaxLength(Slug.MaxLength);
        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.ManagedBy).HasConversion<string>().HasMaxLength(20);

        // A rule aimed at one check goes away with it; selector rules do not depend on any check.
        builder.HasOne<Check>().WithMany().HasForeignKey(r => r.CheckId).OnDelete(DeleteBehavior.Cascade);
    }
}
