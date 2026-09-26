using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Application.Webhooks.ReceiveWebhook;
using Vigia.Plugins;

namespace Vigia.Api.Controllers;

/// <summary>
/// Inbound webhooks declared by check plugins, for example heartbeat pings. No user auth: each check has its own token,
/// sent as <c>?token=</c> or the <c>X-Vigia-Token</c> header, so cron jobs and third parties can call it.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/hooks")]
public sealed class HooksController(IMediator mediator) : ControllerBase
{
    /// <summary>Header that carries the webhook token.</summary>
    public const string TokenHeader = "X-Vigia-Token";

    /// <summary>Largest body accepted, in bytes.</summary>
    public const int MaxBodyBytes = 64 * 1024;

    /// <summary>Receives a request for a check webhook. 204 when accepted.</summary>
    [AcceptVerbs("GET", "POST")]
    [Route("{slug}/{webhook}")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> Receive(string slug, string webhook, CancellationToken ct)
    {
        var token = Request.Query.TryGetValue("token", out var q) ? q.ToString() : Request.Headers[TokenHeader].ToString();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var request = new WebhookRequest(
            Request.Method,
            Request.Query.Where(p => p.Key != "token").ToDictionary(p => p.Key, p => p.Value.ToString()),
            Request.Headers.Where(h => !h.Key.Equals(TokenHeader, StringComparison.OrdinalIgnoreCase)).ToDictionary(h => h.Key, h => h.Value.ToString()),
            body);

        var outcome = await mediator.Send(new ReceiveWebhookCommand(slug, webhook, string.IsNullOrEmpty(token) ? null : token, request), ct);
        return outcome == WebhookOutcome.Accepted ? NoContent() : BadRequest();
    }
}
