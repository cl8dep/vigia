using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Api.Requests;
using Vigia.Application.Workers;
using Vigia.Application.Workers.Assignments;
using Vigia.Application.Workers.Enroll;
using Vigia.Application.Workers.Heartbeat;
using Vigia.Infrastructure.Auth;

namespace Vigia.Api.Controllers;

/// <summary>
/// Endpoints called by remote workers, under their own prefix so this traffic can be split from the user API later.
/// Authenticated with <c>Authorization: Worker &lt;credential&gt;</c>; user tokens are not accepted here.
/// </summary>
[ApiController]
[Authorize(Policy = WorkerAuthentication.Policy)]
[Route("worker/v1")]
public sealed class WorkerProtocolController(IMediator mediator) : ControllerBase
{
    /// <summary>Exchanges a one-time enrollment token for the worker's credential.</summary>
    [HttpPost("enroll")]
    [AllowAnonymous]
    public async Task<ActionResult<EnrollmentDto>> Enroll(EnrollCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>The full set of checks this worker must run.</summary>
    [HttpGet("assignments")]
    public async Task<ActionResult<AssignmentsDto>> Assignments(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetAssignmentsQuery(WorkerId()), ct));
    }

    /// <summary>Reports the worker alive and returns how the control plane sees it.</summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<WorkerDto>> Heartbeat(HeartbeatRequest request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new HeartbeatCommand(WorkerId(), request.Version), ct));
    }

    private Guid WorkerId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
