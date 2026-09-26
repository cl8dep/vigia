using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Application.Plugins;
using Vigia.Application.Plugins.ListPlugins;

namespace Vigia.Api.Controllers;

/// <summary>
/// Installed plugins and their config schemas.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/plugins")]
public sealed class PluginsController(IMediator mediator, IPluginRegistry registry) : ControllerBase
{
    /// <summary>Lists installed plugins with their config schemas.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PluginDto>>> List(CancellationToken ct)
    {
        return Ok(await mediator.Send(new ListPluginsQuery(), ct));
    }

    /// <summary>Lists plugin folders that failed to load and why.</summary>
    [HttpGet("failures")]
    public ActionResult<IReadOnlyCollection<PluginLoadFailure>> Failures()
    {
        return Ok(registry.Failures);
    }
}
