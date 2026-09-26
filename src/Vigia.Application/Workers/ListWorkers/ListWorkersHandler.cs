using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Workers.ListWorkers;

/// <summary>
/// Handles <see cref="ListWorkersQuery"/>.
/// </summary>
public sealed class ListWorkersHandler(IAppDbContext db, TimeProvider time) : IQueryHandler<ListWorkersQuery, IReadOnlyList<WorkerDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<WorkerDto>> Handle(ListWorkersQuery query, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var workers = await db.Workers.AsNoTracking().OrderBy(w => w.Slug).ToListAsync(ct);
        return workers.Select(w => WorkerDto.From(w, now)).ToList();
    }
}
