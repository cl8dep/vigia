using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Checks;
using Vigia.Domain.Results;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="CheckResultRollup"/>.
/// </summary>
public sealed class CheckResultRollupConfiguration : IEntityTypeConfiguration<CheckResultRollup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CheckResultRollup> builder)
    {
        // Raw SQL in the rollup job depends on this name and the key.
        builder.ToTable("check_result_rollups");
        builder.HasKey(r => new { r.CheckId, r.Worker, r.HourStart });
        builder.Property(r => r.Worker).HasMaxLength(100);
        builder.Property(r => r.Dimensions)
            .HasColumnType("jsonb")
            .HasConversion(new JsonDictionaryConverter<DimensionStats>(), new DictionaryComparer<DimensionStats>());
        builder.HasIndex(r => r.HourStart);
        builder.HasOne<Check>().WithMany().HasForeignKey(r => r.CheckId).OnDelete(DeleteBehavior.Cascade);
    }
}
