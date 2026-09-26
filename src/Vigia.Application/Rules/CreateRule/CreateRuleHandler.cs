using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;
using Vigia.Domain.Common;
using Vigia.Domain.Rules;

namespace Vigia.Application.Rules.CreateRule;

/// <summary>
/// Handles <see cref="CreateRuleCommand"/>.
/// </summary>
public sealed class CreateRuleHandler(IAppDbContext db, IPluginRegistry registry) : ICommandHandler<CreateRuleCommand, RuleDto>
{
    /// <inheritdoc />
    public async ValueTask<RuleDto> Handle(CreateRuleCommand command, CancellationToken ct)
    {
        if (await db.Rules.AnyAsync(r => r.Slug == command.Slug, ct))
        {
            throw new ConflictException($"A rule with slug '{command.Slug}' already exists.");
        }

        Rule rule;
        try
        {
            rule = new Rule(command.Slug, command.Spec.Name ?? command.Slug, ManagedBy.Ui);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("slug", ex.Message);
        }

        await RuleSpecApplier.ApplyAsync(rule, command.Slug, command.Spec, db, registry, ct);
        db.Rules.Add(rule);
        await db.SaveChangesAsync(ct);
        return RuleDto.From(rule, command.Spec.Check);
    }
}
