using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;

namespace Vigia.Application.Services.ListHealthChanges;

/// <summary>
/// Handles <see cref="ListHealthChangesQuery"/>.
/// </summary>
public sealed class ListHealthChangesHandler(IAppDbContext db) : IQueryHandler<ListHealthChangesQuery, IReadOnlyList<HealthChangeDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<HealthChangeDto>> Handle(ListHealthChangesQuery query, CancellationToken ct)
    {
        if (query.Limit is < 1 or > 500)
        {
            throw new ValidationException("limit", "Limit must be between 1 and 500.");
        }

        var serviceId = await db.Services.Where(s => s.Slug == query.Slug).Select(s => (Guid?)s.Id).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("service", query.Slug);

        var changes = await db.ServiceHealthChanges.AsNoTracking()
            .Where(c => c.ServiceId == serviceId)
            .OrderByDescending(c => c.At)
            .ThenByDescending(c => c.Id)
            .Take(query.Limit)
            .ToListAsync(ct);

        return changes
            .Select(c => new HealthChangeDto(c.From is null ? null : HealthNames.Of(c.From.Value), HealthNames.Of(c.To), c.Reason, c.At))
            .ToList();
    }
}
