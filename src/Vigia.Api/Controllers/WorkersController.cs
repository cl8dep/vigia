using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Api.Requests;
using Vigia.Application.Workers;
using Vigia.Application.Workers.CreateWorker;
using Vigia.Application.Workers.DeleteWorker;
using Vigia.Application.Workers.GetWorker;
using Vigia.Application.Workers.IssueEnrollmentToken;
using Vigia.Application.Workers.ListWorkers;
using Vigia.Application.Workers.RevokeWorker;
using Vigia.Application.Workers.UpdateWorker;

namespace Vigia.Api.Controllers;

/// <summary>
/// Worker management for users: register workers, issue enrollment tokens, revoke credentials.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/workers")]
public sealed class WorkersController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all workers, including the built-in one.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkerDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new ListWorkersQuery(), ct));
    }

    /// <summary>Gets a worker by slug.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<WorkerDto>> Get(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetWorkerQuery(slug), ct));
    }

    /// <summary>Registers a remote worker. The response carries a one-time enrollment token, valid 24 hours.</summary>
    [HttpPost]
    public async Task<ActionResult<WorkerDto>> Create(CreateWorkerRequest request, CancellationToken ct)
    {
        var worker = await mediator.Send(new CreateWorkerCommand(request.Slug, request), ct);
        return CreatedAtAction(nameof(Get), new { slug = worker.Slug }, worker);
    }

    /// <summary>Replaces a worker's name, region and tags.</summary>
    [HttpPut("{slug}")]
    public async Task<ActionResult<WorkerDto>> Update(string slug, WorkerSpec spec, CancellationToken ct)
    {
        return Ok(await mediator.Send(new UpdateWorkerCommand(slug, spec), ct));
    }

    /// <summary>Deletes a remote worker; its credential stops working.</summary>
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        await mediator.Send(new DeleteWorkerCommand(slug), ct);
        return NoContent();
    }

    /// <summary>Issues a new one-time enrollment token, for example to reinstall the worker.</summary>
    [HttpPost("{slug}/enrollment-token")]
    public async Task<ActionResult<WorkerDto>> IssueEnrollmentToken(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new IssueEnrollmentTokenCommand(slug), ct));
    }

    /// <summary>Invalidates the worker's credential immediately.</summary>
    [HttpPost("{slug}/revoke")]
    public async Task<ActionResult<WorkerDto>> Revoke(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new RevokeWorkerCommand(slug), ct));
    }
}
