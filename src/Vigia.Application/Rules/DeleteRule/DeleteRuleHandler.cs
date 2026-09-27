using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Health;

namespace Vigia.Application.Rules.DeleteRule;

/// <summary>
/// Handles <see cref="DeleteRuleCommand"/>. Alerts go with it (cascade).
/// </summary>
public sealed class DeleteRuleHandler(IAppDbContext db, IServiceHealthUpdater health) : ICommandHandler<DeleteRuleCommand>
{
    /// <inheritdoc />
    public async ValueTask<Unit> Handle(DeleteRuleCommand command, CancellationToken ct)
    {
        var rule = await db.Rules.SingleOrDefaultAsync(r => r.Slug == command.Slug, ct)
            ?? throw new NotFoundException("rule", command.Slug);

        db.Rules.Remove(rule);
        await db.SaveChangesAsync(ct);

        // Membership, alerts or the graph may have changed.
        await health.RecomputeAsync(ct);
        return Unit.Value;
    }
}
