using Microsoft.EntityFrameworkCore;
using Vigia.Domain.Checks;
using Vigia.Domain.Results;

namespace Vigia.Application.Common.Interfaces;

/// <summary>
/// Persistence as seen by use cases.
/// </summary>
public interface IAppDbContext
{
    DbSet<Check> Checks { get; }

    DbSet<CheckResult> CheckResults { get; }

    DbSet<CheckResultRollup> CheckResultRollups { get; }

    /// <summary>Persists pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
