using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Common;
using Vigia.Domain.Workers;

namespace Vigia.Application.Workers;

/// <summary>
/// Keeps the built-in worker's row in sync with configuration and records that it is alive.
/// </summary>
public sealed class BuiltInWorkerRegistration(IAppDbContext db, IOptions<WorkerOptions> options, TimeProvider time)
{
    /// <summary>Creates or updates the built-in worker (slug <see cref="WorkerOptions.Name"/>) and marks it seen.</summary>
    public async Task EnsureAsync(string? version, CancellationToken ct)
    {
        var opts = options.Value;
        var worker = await db.Workers.SingleOrDefaultAsync(w => w.Slug == opts.Name, ct);
        if (worker is null)
        {
            worker = new Worker(opts.Name, "Built-in", opts.Region, builtIn: true, ManagedBy.Ui);
            db.Workers.Add(worker);
        }
        else if (!worker.BuiltIn)
        {
            throw new InvalidOperationException($"Worker slug '{opts.Name}' belongs to a remote worker; set Worker:Name to another slug.");
        }
        else
        {
            worker.MoveTo(opts.Region);
        }

        worker.Seen(time.GetUtcNow(), version);
        await db.SaveChangesAsync(ct);
    }
}
