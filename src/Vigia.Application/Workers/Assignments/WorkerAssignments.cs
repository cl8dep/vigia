using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Placement;
using Vigia.Application.Plugins;
using Vigia.Domain.Checks;
using Vigia.Domain.Workers;

namespace Vigia.Application.Workers.Assignments;

/// <summary>
/// Enabled checks a given worker should run.
/// </summary>
public static class WorkerAssignments
{
    /// <summary>Checks for <paramref name="worker"/>, ordered by slug.</summary>
    public static async Task<IReadOnlyList<Check>> ForAsync(Worker worker, IAppDbContext db, IPluginRegistry registry, CancellationToken ct)
    {
        var checks = await db.Checks.AsNoTracking().Where(c => c.Enabled).OrderBy(c => c.Slug).ToListAsync(ct);
        return checks
            .Where(c => CheckPlacement.CanRun(c, registry.TryGetCheck(c.Plugin, out var plugin) ? plugin : null, worker))
            .ToList();
    }
}
