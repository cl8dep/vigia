using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Agents;
using Vigia.Domain.Results;
using Vigia.Infrastructure.Persistence;

namespace Vigia.Infrastructure.Agents;

/// <summary>
/// Stores the built-in agent's results directly in the database.
/// </summary>
public sealed class DbResultSink(IServiceScopeFactory scopes) : IResultSink
{
    /// <inheritdoc />
    public async Task WriteAsync(ProbeRecord record, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckResults.Add(new CheckResult(
            record.Id,
            record.CheckId,
            record.Agent,
            Enum.Parse<ResultOutcome>(record.Outcome.ToString()),
            record.Measurements,
            record.Message,
            record.DurationMs,
            record.ObservedAt));
        await db.SaveChangesAsync(ct);
    }
}
