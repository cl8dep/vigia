using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vigia.Domain.Common;

namespace Vigia.Infrastructure.Persistence;

/// <summary>
/// Sets <see cref="Entity.CreatedAt"/> and <see cref="Entity.UpdatedAt"/> on save.
/// </summary>
public sealed class TimestampInterceptor(TimeProvider time) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var now = time.GetUtcNow();
            foreach (var entry in eventData.Context.ChangeTracker.Entries<Entity>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.Touch(now, entry.State == EntityState.Added);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
