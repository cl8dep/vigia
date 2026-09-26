using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Alerts;
using Vigia.Domain.Checks;
using Vigia.Domain.Results;
using Vigia.Domain.Rules;
using Vigia.Domain.Webhooks;
using Vigia.Infrastructure.Identity;

namespace Vigia.Infrastructure.Persistence;

/// <summary>
/// EF Core context: domain entities plus Identity tables.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public DbSet<Check> Checks
    {
        get { return Set<Check>(); }
    }

    public DbSet<CheckResult> CheckResults
    {
        get { return Set<CheckResult>(); }
    }

    public DbSet<CheckResultRollup> CheckResultRollups
    {
        get { return Set<CheckResultRollup>(); }
    }

    public DbSet<Rule> Rules
    {
        get { return Set<Rule>(); }
    }

    public DbSet<Alert> Alerts
    {
        get { return Set<Alert>(); }
    }

    public DbSet<WebhookReceipt> WebhookReceipts
    {
        get { return Set<WebhookReceipt>(); }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
