using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vigia.Application.Plugins;
using Vigia.Application.Workers;
using Vigia.Application.Workers.Assignments;
using Vigia.Domain.Common;
using Vigia.Domain.Workers;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Workers;

/// <summary>
/// Assignments for the built-in worker: enabled checks whose placement matches its tags, read from the database.
/// </summary>
public sealed class DbAssignmentSource(IServiceScopeFactory scopes, IPluginRegistry registry, IOptions<WorkerOptions> options) : IAssignmentSource
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckAssignment>> GetAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var opts = options.Value;

        // Before its first registration the built-in worker still knows its own tags from configuration.
        var self = await db.Workers.AsNoTracking().SingleOrDefaultAsync(w => w.Slug == opts.Name && w.BuiltIn, ct)
            ?? new Worker(opts.Name, "Built-in", opts.Region, builtIn: true, ManagedBy.Ui);

        var checks = await WorkerAssignments.ForAsync(self, db, registry, ct);
        return checks.Select(CheckAssignment.From).ToList();
    }
}
