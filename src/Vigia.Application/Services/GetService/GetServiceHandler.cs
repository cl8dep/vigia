using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Services.GetService;

/// <summary>
/// Handles <see cref="GetServiceQuery"/>.
/// </summary>
public sealed class GetServiceHandler(IAppDbContext db) : IQueryHandler<GetServiceQuery, ServiceDto>
{
    /// <inheritdoc />
    public async ValueTask<ServiceDto> Handle(GetServiceQuery query, CancellationToken ct)
    {
        var service = await db.Services.AsNoTracking().Include(s => s.Dependencies).SingleOrDefaultAsync(s => s.Slug == query.Slug, ct)
            ?? throw new NotFoundException("service", query.Slug);
        return (await ServiceViews.BuildAsync(db, [service], ct))[0];
    }
}
