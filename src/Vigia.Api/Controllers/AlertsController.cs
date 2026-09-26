using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Application.Alerts;
using Vigia.Application.Alerts.GetAlert;
using Vigia.Application.Alerts.ListAlerts;

namespace Vigia.Api.Controllers;

/// <summary>
/// Alerts opened by rules.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/alerts")]
public sealed class AlertsController(IMediator mediator) : ControllerBase
{
    /// <summary>Alerts newest first, filtered by <c>state</c> (firing, resolved) and <c>check</c> slug.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertDto>>> List(
        CancellationToken ct, [FromQuery] string? state = null, [FromQuery] string? check = null, [FromQuery] int limit = 50)
    {
        return Ok(await mediator.Send(new ListAlertsQuery(state, check, limit), ct));
    }

    /// <summary>Gets an alert by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAlertQuery(id), ct));
    }
}
