using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Api.Requests;
using Vigia.Application.Rules;
using Vigia.Application.Rules.CreateRule;
using Vigia.Application.Rules.DeleteRule;
using Vigia.Application.Rules.GetRule;
using Vigia.Application.Rules.ListRules;
using Vigia.Application.Rules.UpdateRule;

namespace Vigia.Api.Controllers;

/// <summary>
/// Alert rules.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/rules")]
public sealed class RulesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all rules.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RuleDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new ListRulesQuery(), ct));
    }

    /// <summary>Gets a rule by slug.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<RuleDto>> Get(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetRuleQuery(slug), ct));
    }

    /// <summary>Creates a rule.</summary>
    [HttpPost]
    public async Task<ActionResult<RuleDto>> Create(CreateRuleRequest request, CancellationToken ct)
    {
        var rule = await mediator.Send(new CreateRuleCommand(request.Slug, request), ct);
        return CreatedAtAction(nameof(Get), new { slug = rule.Slug }, rule);
    }

    /// <summary>Replaces a rule's definition.</summary>
    [HttpPut("{slug}")]
    public async Task<ActionResult<RuleDto>> Update(string slug, RuleSpec spec, CancellationToken ct)
    {
        return Ok(await mediator.Send(new UpdateRuleCommand(slug, spec), ct));
    }

    /// <summary>Deletes a rule and its alerts.</summary>
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        await mediator.Send(new DeleteRuleCommand(slug), ct);
        return NoContent();
    }
}
