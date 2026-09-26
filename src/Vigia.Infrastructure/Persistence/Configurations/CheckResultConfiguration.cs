using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Checks;
using Vigia.Domain.Results;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="CheckResult"/>.
/// </summary>
public sealed class CheckResultConfiguration : IEntityTypeConfiguration<CheckResult>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CheckResult> builder)
    {
        // Raw SQL in the rollup job depends on this name.
        builder.ToTable("check_results");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Worker).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.Measurements)
            .HasColumnType("jsonb")
            .HasConversion(new JsonDictionaryConverter<double>(), new DictionaryComparer<double>());
        builder.Property(r => r.Message).HasMaxLength(2000);

        // Latest results of a check first: the only access pattern for evaluation and the API.
        builder.HasIndex(r => new { r.CheckId, r.ObservedAt }).IsDescending(false, true);

        // Retention and rollups scan by time across all checks.
        builder.HasIndex(r => r.ObservedAt);

        builder.HasOne<Check>().WithMany().HasForeignKey(r => r.CheckId).OnDelete(DeleteBehavior.Cascade);
    }
}
