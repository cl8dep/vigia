using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;
using Vigia.Domain.Health;

namespace Vigia.Application.StatusPages.PublicStatus;

/// <summary>
/// Handles <see cref="GetPublicStatusQuery"/> from stored health and its transition history.
/// </summary>
public sealed class GetPublicStatusHandler(IAppDbContext db, TimeProvider time) : IQueryHandler<GetPublicStatusQuery, PublicStatusDto>
{
    /// <inheritdoc />
    public async ValueTask<PublicStatusDto> Handle(GetPublicStatusQuery query, CancellationToken ct)
    {
        if (query.Days is < 1 or > 90)
        {
            throw new ValidationException("days", "Days must be between 1 and 90.");
        }

        var page = await db.StatusPages.AsNoTracking().Include(p => p.Components).SingleOrDefaultAsync(p => p.Slug == query.Slug, ct)
            ?? throw new NotFoundException("status page", query.Slug);

        var now = time.GetUtcNow();
        var ids = page.Components.Select(c => c.ServiceId).ToList();
        var health = await db.ServiceHealth.AsNoTracking().Where(h => ids.Contains(h.ServiceId)).ToDictionaryAsync(h => h.ServiceId, ct);

        // The last change before the window tells the state at its start, so load from a little earlier.
        var windowStart = now.AddDays(-query.Days - 1);
        var changes = (await db.ServiceHealthChanges.AsNoTracking().Where(c => ids.Contains(c.ServiceId)).ToListAsync(ct))
            .GroupBy(c => c.ServiceId)
            .ToDictionary(g => g.Key, g => KeepFrom(g.ToList(), windowStart));

        var components = page.Components.OrderBy(c => c.Order).Select(c =>
        {
            var snapshot = health.GetValueOrDefault(c.ServiceId);
            var days = AvailabilityHistory.Daily(changes.GetValueOrDefault(c.ServiceId, []), query.Days, now);
            var known = days.Where(d => d.Uptime is not null).ToList();
            return new PublicComponentDto(
                c.Name,
                c.Group,
                HealthNames.Of(snapshot?.State ?? HealthState.Unknown),
                snapshot?.Since,
                snapshot?.Partitions.ToDictionary(p => p.Key, p => HealthNames.Of(p.Value)) ?? [],
                known.Count == 0 ? null : known.Average(d => d.Uptime!.Value),
                [.. days.Select(d => new PublicDayDto(d.Date.ToString("yyyy-MM-dd"), d.State is null ? null : HealthNames.Of(d.State.Value), d.Uptime))]);
        }).ToList();

        var overall = page.Components.Count == 0
            ? HealthState.Unknown
            : page.Components.Select(c => health.GetValueOrDefault(c.ServiceId)?.State ?? HealthState.Unknown).Max();
        return new PublicStatusDto(page.Name, page.Description, HealthNames.Of(overall), components, now);
    }

    private static List<ServiceHealthChange> KeepFrom(List<ServiceHealthChange> changes, DateTimeOffset start)
    {
        var before = changes.Where(c => c.At < start).OrderByDescending(c => c.At).FirstOrDefault();
        return [.. changes.Where(c => c.At >= start).Concat(before is null ? [] : [before])];
    }
}
