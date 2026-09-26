using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Alerts;
using Vigia.Domain.Checks;
using Vigia.Domain.Rules;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Alert"/>.
/// </summary>
public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    /// <summary>Name of the index that allows one firing alert per rule and check.</summary>
    public const string OneFiringPerRuleAndCheck = "ux_alerts_firing_rule_check";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Severity).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.State).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.Message).HasMaxLength(2000);

        // Postgres xmin as optimistic concurrency token: concurrent results updating the same alert retry instead of
        // silently overwriting each other's counters.
        builder.Property<uint>("Version").IsRowVersion();

        // The database, not the evaluator, guarantees there is never a second firing alert for the same pair.
        builder.HasIndex(a => new { a.RuleId, a.CheckId })
            .HasDatabaseName(OneFiringPerRuleAndCheck)
            .IsUnique()
            .HasFilter("state = 'Firing'");
        builder.HasIndex(a => new { a.State, a.FiredAt });

        builder.HasOne<Rule>().WithMany().HasForeignKey(a => a.RuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Check>().WithMany().HasForeignKey(a => a.CheckId).OnDelete(DeleteBehavior.Cascade);
    }
}
