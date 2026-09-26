using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Rules.GetRule;

/// <summary>
/// Handles <see cref="GetRuleQuery"/>.
/// </summary>
public sealed class GetRuleHandler(IAppDbContext db) : IQueryHandler<GetRuleQuery, RuleDto>
{
    /// <inheritdoc />
    public async ValueTask<RuleDto> Handle(GetRuleQuery query, CancellationToken ct)
    {
        var rule = await db.Rules.AsNoTracking().SingleOrDefaultAsync(r => r.Slug == query.Slug, ct)
            ?? throw new NotFoundException("rule", query.Slug);

        var checkSlug = rule.CheckId is null
            ? null
            : await db.Checks.Where(c => c.Id == rule.CheckId).Select(c => c.Slug).SingleOrDefaultAsync(ct);
        return RuleDto.From(rule, checkSlug);
    }
}
