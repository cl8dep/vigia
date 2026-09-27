using Microsoft.EntityFrameworkCore;
using Vigia.Domain.Alerts;
using Vigia.Domain.Checks;
using Vigia.Domain.Results;
using Vigia.Domain.Rules;
using Vigia.Domain.Services;
using Vigia.Domain.Workers;

namespace Vigia.Application.Common.Interfaces;

/// <summary>
/// Persistence as seen by use cases.
/// </summary>
public interface IAppDbContext
{
    DbSet<Check> Checks { get; }

    DbSet<CheckResult> CheckResults { get; }

    DbSet<CheckResultRollup> CheckResultRollups { get; }

    DbSet<Rule> Rules { get; }

    DbSet<Alert> Alerts { get; }

    DbSet<Worker> Workers { get; }

    DbSet<Service> Services { get; }

    DbSet<ServiceDependency> ServiceDependencies { get; }

    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
