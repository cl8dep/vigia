using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Checks.ListCheckRollups;

/// <summary>
/// Handles <see cref="ListCheckRollupsQuery"/>.
/// </summary>
public sealed class ListCheckRollupsHandler(IAppDbContext db, TimeProvider time) : IQueryHandler<ListCheckRollupsQuery, IReadOnlyList<CheckRollupDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CheckRollupDto>> Handle(ListCheckRollupsQuery query, CancellationToken ct)
    {
        var to = query.To ?? time.GetUtcNow();
        var from = query.From ?? to.AddHours(-24);
        if (from >= to)
        {
            throw new ValidationException("from", "'from' must be before 'to'.");
        }

        if (to - from > ListCheckRollupsQuery.MaxRange)
        {
            throw new ValidationException("from", $"Range cannot exceed {ListCheckRollupsQuery.MaxRange.TotalDays} days.");
        }

        var checkId = await db.Checks.Where(c => c.Slug == query.Slug).Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("check", query.Slug);

        var rollups = await db.CheckResultRollups.AsNoTracking()
            .Where(r => r.CheckId == checkId && r.HourStart >= from && r.HourStart < to)
            .OrderBy(r => r.HourStart)
            .ThenBy(r => r.Agent)
            .ToListAsync(ct);

        return rollups
            .Select(r => new CheckRollupDto(r.Agent, r.HourStart, r.Up, r.Down, r.Error, r.Dimensions))
            .ToList();
    }
}
