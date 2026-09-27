using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Application.StatusPages.PublicStatus;

namespace Vigia.Api.Controllers;

/// <summary>
/// Public, unauthenticated, read-only status pages. Only what a page lists, under its public names.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("status")]
public sealed class PublicStatusController(IMediator mediator) : ControllerBase
{
    /// <summary>Current status and daily history (<c>days</c>, default 90) of a status page.</summary>
    [HttpGet("{slug}")]
    [ResponseCache(Duration = 15)]
    public async Task<ActionResult<PublicStatusDto>> Get(string slug, CancellationToken ct, [FromQuery] int days = 90)
    {
        return Ok(await mediator.Send(new GetPublicStatusQuery(slug, days), ct));
    }
}
