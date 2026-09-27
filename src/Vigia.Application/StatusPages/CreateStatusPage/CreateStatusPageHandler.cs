using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Common;
using Vigia.Domain.StatusPages;

namespace Vigia.Application.StatusPages.CreateStatusPage;

/// <summary>
/// Handles <see cref="CreateStatusPageCommand"/>.
/// </summary>
public sealed class CreateStatusPageHandler(IAppDbContext db) : ICommandHandler<CreateStatusPageCommand, StatusPageDto>
{
    /// <inheritdoc />
    public async ValueTask<StatusPageDto> Handle(CreateStatusPageCommand command, CancellationToken ct)
    {
        if (await db.StatusPages.AnyAsync(p => p.Slug == command.Slug, ct))
        {
            throw new ConflictException($"A status page with slug '{command.Slug}' already exists.");
        }

        StatusPage page;
        try
        {
            page = new StatusPage(command.Slug, command.Spec.Title ?? command.Slug, ManagedBy.Ui);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("slug", ex.Message);
        }

        await StatusPageViews.ApplyAsync(page, command.Slug, command.Spec, db, ct);
        db.StatusPages.Add(page);
        await db.SaveChangesAsync(ct);
        return await StatusPageViews.ToDtoAsync(page, db, ct);
    }
}
