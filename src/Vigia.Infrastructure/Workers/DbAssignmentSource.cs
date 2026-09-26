using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Workers;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Workers;

/// <summary>
/// Assignments for the built-in worker: every enabled check, read straight from the database.
/// </summary>
public sealed class DbAssignmentSource(IServiceScopeFactory scopes) : IAssignmentSource
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckAssignment>> GetAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checks = await db.Checks.AsNoTracking().Where(c => c.Enabled).ToListAsync(ct);
        return checks.Select(CheckAssignment.From).ToList();
    }
}
