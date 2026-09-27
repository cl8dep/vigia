using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Common;
using Vigia.Domain.Services;

namespace Vigia.Application.Services.CreateService;

/// <summary>
/// Handles <see cref="CreateServiceCommand"/>.
/// </summary>
public sealed class CreateServiceHandler(IAppDbContext db) : ICommandHandler<CreateServiceCommand, ServiceDto>
{
    /// <inheritdoc />
    public async ValueTask<ServiceDto> Handle(CreateServiceCommand command, CancellationToken ct)
    {
        if (await db.Services.AnyAsync(s => s.Slug == command.Slug, ct))
        {
            throw new ConflictException($"A service with slug '{command.Slug}' already exists.");
        }

        Service service;
        try
        {
            service = new Service(command.Slug, command.Spec.Name ?? command.Slug, ManagedBy.Ui);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("slug", ex.Message);
        }

        await ServiceSpecApplier.ApplyAsync(service, command.Slug, command.Spec, db, ct);
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        return (await ServiceViews.BuildAsync(db, [service], ct))[0];
    }
}
