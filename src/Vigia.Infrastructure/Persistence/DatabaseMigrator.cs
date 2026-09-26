using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vigia.Infrastructure.Persistence;

/// <summary>
/// Applies EF Core migrations at startup when <see cref="DatabaseOptions.MigrateOnStartup"/> is on.
/// </summary>
public static class DatabaseMigrator
{
    /// <summary>Applies pending migrations, if enabled.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        if (!scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateOnStartup)
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));
        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        await db.Database.MigrateAsync(ct);
    }
}
