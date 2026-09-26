using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Rules.ListRules;

/// <summary>
/// Handles <see cref="ListRulesQuery"/>.
/// </summary>
public sealed class ListRulesHandler(IAppDbContext db) : IQueryHandler<ListRulesQuery, IReadOnlyList<RuleDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<RuleDto>> Handle(ListRulesQuery query, CancellationToken ct)
    {
        var rules = await db.Rules.AsNoTracking().OrderBy(r => r.Slug).ToListAsync(ct);
        var checkIds = rules.Where(r => r.CheckId is not null).Select(r => r.CheckId!.Value).ToList();
        var slugs = await db.Checks.Where(c => checkIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Slug, ct);

        return rules.Select(r => RuleDto.From(r, r.CheckId is null ? null : slugs.GetValueOrDefault(r.CheckId.Value))).ToList();
    }
}
