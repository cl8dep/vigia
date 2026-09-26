using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Application.Checks;
using Vigia.Api.Requests;
using Vigia.Application.Checks.CreateCheck;
using Vigia.Application.Checks.DeleteCheck;
using Vigia.Application.Checks.GetCheck;
using Vigia.Application.Checks.ListCheckResults;
using Vigia.Application.Checks.ListCheckRollups;
using Vigia.Application.Checks.ListChecks;
using Vigia.Application.Checks.ProbeCheck;
using Vigia.Application.Checks.UpdateCheck;
using Vigia.Application.Plugins.ValidateCheckConfig;

namespace Vigia.Api.Controllers;

/// <summary>
/// Checks: create, read, validate config and probe on demand.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/checks")]
public sealed class ChecksController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all checks.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CheckDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new ListChecksQuery(), ct));
    }

    /// <summary>Gets a check by slug.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<CheckDto>> Get(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCheckQuery(slug), ct));
    }

    /// <summary>Creates a check.</summary>
    [HttpPost]
    public async Task<ActionResult<CheckDto>> Create(CreateCheckCommand command, CancellationToken ct)
    {
        var check = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { slug = check.Slug }, check);
    }

    /// <summary>
    /// Replaces a check's settings. Omitted optional fields go back to defaults; omitted secret config fields keep their value.
    /// </summary>
    [HttpPut("{slug}")]
    public async Task<ActionResult<CheckDto>> Update(string slug, UpdateCheckRequest request, CancellationToken ct)
    {
        var command = new UpdateCheckCommand(slug, request.Name, request.Plugin, request.Config, request.Interval, request.Labels, request.Enabled);
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>Deletes a check with its results and rollups.</summary>
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        await mediator.Send(new DeleteCheckCommand(slug), ct);
        return NoContent();
    }

    /// <summary>
    /// Validates a check config against the installed plugin without saving. Returns the normalized config.
    /// Used by the CLI and the Terraform provider at plan time.
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<System.Text.Json.JsonElement>> Validate(ValidateCheckConfigQuery query, CancellationToken ct)
    {
        return Ok(await mediator.Send(query, ct));
    }

    /// <summary>Latest stored results of a check, newest first.</summary>
    [HttpGet("{slug}/results")]
    public async Task<ActionResult<IReadOnlyList<CheckResultDto>>> Results(string slug, CancellationToken ct, [FromQuery] int limit = 50)
    {
        return Ok(await mediator.Send(new ListCheckResultsQuery(slug, limit), ct));
    }

    /// <summary>Hourly rollups of a check, oldest first. Defaults to the last 24 hours.</summary>
    [HttpGet("{slug}/rollups")]
    public async Task<ActionResult<IReadOnlyList<CheckRollupDto>>> Rollups(
        string slug, CancellationToken ct, [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null)
    {
        return Ok(await mediator.Send(new ListCheckRollupsQuery(slug, from, to), ct));
    }

    /// <summary>Runs one probe now, in-process, without storing the result.</summary>
    [HttpPost("{slug}/probe")]
    public async Task<ActionResult<ProbeResultDto>> Probe(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ProbeCheckCommand(slug), ct));
    }
}
