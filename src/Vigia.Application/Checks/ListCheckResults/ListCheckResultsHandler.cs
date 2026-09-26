using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Checks.ListCheckResults;

/// <summary>
/// Handles <see cref="ListCheckResultsQuery"/>.
/// </summary>
public sealed class ListCheckResultsHandler(IAppDbContext db) : IQueryHandler<ListCheckResultsQuery, IReadOnlyList<CheckResultDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<CheckResultDto>> Handle(ListCheckResultsQuery query, CancellationToken ct)
    {
        if (query.Limit is < 1 or > ListCheckResultsQuery.MaxLimit)
        {
            throw new ValidationException("limit", $"Limit must be between 1 and {ListCheckResultsQuery.MaxLimit}.");
        }

        var checkId = await db.Checks.Where(c => c.Slug == query.Slug).Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("check", query.Slug);

        var results = await db.CheckResults.AsNoTracking()
            .Where(r => r.CheckId == checkId)
            .OrderByDescending(r => r.ObservedAt)
            .Take(query.Limit)
            .ToListAsync(ct);

        return results
            .Select(r => new CheckResultDto(
                r.Id,
                r.Worker,
                JsonNamingPolicy.CamelCase.ConvertName(r.Outcome.ToString()),
                r.Measurements,
                r.Message,
                r.DurationMs,
                r.ObservedAt))
            .ToList();
    }
}
