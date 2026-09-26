using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Workers.GetWorker;

/// <summary>
/// Handles <see cref="GetWorkerQuery"/>.
/// </summary>
public sealed class GetWorkerHandler(IAppDbContext db, TimeProvider time) : IQueryHandler<GetWorkerQuery, WorkerDto>
{
    /// <inheritdoc />
    public async ValueTask<WorkerDto> Handle(GetWorkerQuery query, CancellationToken ct)
    {
        var worker = await db.Workers.AsNoTracking().SingleOrDefaultAsync(w => w.Slug == query.Slug, ct)
            ?? throw new NotFoundException("worker", query.Slug);
        return WorkerDto.From(worker, time.GetUtcNow());
    }
}
