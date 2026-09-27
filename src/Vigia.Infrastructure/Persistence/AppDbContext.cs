using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Alerts;
using Vigia.Domain.Checks;
using Vigia.Domain.Health;
using Vigia.Domain.Results;
using Vigia.Domain.Rules;
using Vigia.Domain.Services;
using Vigia.Domain.StatusPages;
using Vigia.Domain.Webhooks;
using Vigia.Domain.Workers;
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

    public DbSet<Worker> Workers
    {
        get { return Set<Worker>(); }
    }

    public DbSet<Service> Services
    {
        get { return Set<Service>(); }
    }

    public DbSet<ServiceDependency> ServiceDependencies
    {
        get { return Set<ServiceDependency>(); }
    }

    public DbSet<ServiceHealth> ServiceHealth
    {
        get { return Set<ServiceHealth>(); }
    }

    public DbSet<ServiceHealthChange> ServiceHealthChanges
    {
        get { return Set<ServiceHealthChange>(); }
    }

    public DbSet<StatusPage> StatusPages
    {
        get { return Set<StatusPage>(); }
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
