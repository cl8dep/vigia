using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Api.Requests;
using Vigia.Application.Services;
using Vigia.Application.Services.CreateService;
using Vigia.Application.Services.DeleteService;
using Vigia.Application.Services.GetService;
using Vigia.Application.Services.ListServices;
using Vigia.Application.Services.UpdateService;

namespace Vigia.Api.Controllers;

/// <summary>
/// Services: checks grouped by tag selector, with dependencies.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/services")]
public sealed class ServicesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all services.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new ListServicesQuery(), ct));
    }

    /// <summary>Gets a service by slug, with the checks it matches.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ServiceDto>> Get(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetServiceQuery(slug), ct));
    }

    /// <summary>Creates a service.</summary>
    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create(CreateServiceRequest request, CancellationToken ct)
    {
        var service = await mediator.Send(new CreateServiceCommand(request.Slug, request), ct);
        return CreatedAtAction(nameof(Get), new { slug = service.Slug }, service);
    }

    /// <summary>Replaces a service's definition, dependencies included.</summary>
    [HttpPut("{slug}")]
    public async Task<ActionResult<ServiceDto>> Update(string slug, ServiceSpec spec, CancellationToken ct)
    {
        return Ok(await mediator.Send(new UpdateServiceCommand(slug, spec), ct));
    }

    /// <summary>Deletes a service; edges pointing at it go too.</summary>
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        await mediator.Send(new DeleteServiceCommand(slug), ct);
        return NoContent();
    }
}
