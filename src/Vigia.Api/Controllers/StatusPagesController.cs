using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Api.Requests;
using Vigia.Application.StatusPages;
using Vigia.Application.StatusPages.CreateStatusPage;
using Vigia.Application.StatusPages.DeleteStatusPage;
using Vigia.Application.StatusPages.GetStatusPage;
using Vigia.Application.StatusPages.ListStatusPages;
using Vigia.Application.StatusPages.UpdateStatusPage;

namespace Vigia.Api.Controllers;

/// <summary>
/// Status page configuration for users. The public view is served by <see cref="PublicStatusController"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/status-pages")]
public sealed class StatusPagesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all status pages.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StatusPageDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new ListStatusPagesQuery(), ct));
    }

    /// <summary>Gets a status page's configuration.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<StatusPageDto>> Get(string slug, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetStatusPageQuery(slug), ct));
    }

    /// <summary>Creates a status page.</summary>
    [HttpPost]
    public async Task<ActionResult<StatusPageDto>> Create(CreateStatusPageRequest request, CancellationToken ct)
    {
        var page = await mediator.Send(new CreateStatusPageCommand(request.Slug, request), ct);
        return CreatedAtAction(nameof(Get), new { slug = page.Slug }, page);
    }

    /// <summary>Replaces a status page's definition.</summary>
    [HttpPut("{slug}")]
    public async Task<ActionResult<StatusPageDto>> Update(string slug, StatusPageSpec spec, CancellationToken ct)
    {
        return Ok(await mediator.Send(new UpdateStatusPageCommand(slug, spec), ct));
    }

    /// <summary>Deletes a status page.</summary>
    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        await mediator.Send(new DeleteStatusPageCommand(slug), ct);
        return NoContent();
    }
}
