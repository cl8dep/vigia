using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;

namespace Vigia.Application.Rules.UpdateRule;

/// <summary>
/// Handles <see cref="UpdateRuleCommand"/>.
/// </summary>
public sealed class UpdateRuleHandler(IAppDbContext db, IPluginRegistry registry) : ICommandHandler<UpdateRuleCommand, RuleDto>
{
    /// <inheritdoc />
    public async ValueTask<RuleDto> Handle(UpdateRuleCommand command, CancellationToken ct)
    {
        var rule = await db.Rules.SingleOrDefaultAsync(r => r.Slug == command.Slug, ct)
            ?? throw new NotFoundException("rule", command.Slug);

        await RuleSpecApplier.ApplyAsync(rule, command.Slug, command.Spec, db, registry, ct);
        await db.SaveChangesAsync(ct);
        return RuleDto.From(rule, command.Spec.Check);
    }
}
