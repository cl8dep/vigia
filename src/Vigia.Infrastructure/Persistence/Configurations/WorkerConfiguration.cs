using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vigia.Domain.Common;
using Vigia.Domain.Workers;
using Vigia.Infrastructure.Persistence.Conversions;

namespace Vigia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Worker"/>.
/// </summary>
public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.HasIndex(w => w.Slug).IsUnique();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Tags).HasColumnType("jsonb").HasConversion(new JsonDictionaryConverter<string?>(), new DictionaryComparer<string?>());
        builder.Property(w => w.Region).HasMaxLength(255);
        builder.Property(w => w.CredentialHash).HasMaxLength(64);
        builder.Property(w => w.EnrollmentTokenHash).HasMaxLength(64);
        builder.Property(w => w.Version).HasMaxLength(100);
        builder.Property(w => w.ManagedBy).HasConversion<string>().HasMaxLength(20);

        // Workers authenticate by looking up the hash of their secret.
        builder.HasIndex(w => w.CredentialHash).IsUnique().HasFilter("credential_hash IS NOT NULL");
        builder.HasIndex(w => w.EnrollmentTokenHash).IsUnique().HasFilter("enrollment_token_hash IS NOT NULL");
    }
}
