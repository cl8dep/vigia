using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Workers;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;
using Vigia.Plugins;

namespace Vigia.Application.Webhooks.ReceiveWebhook;

/// <summary>
/// Handles <see cref="ReceiveWebhookCommand"/>: resolves the check and its plugin's handler, authenticates, dispatches,
/// and records the receipt when the plugin accepts the request.
/// </summary>
public sealed class ReceiveWebhookHandler(
    IAppDbContext db,
    IPluginRegistry registry,
    IWebhookReceiptStore receipts,
    IResultSink sink,
    TimeProvider time) : ICommandHandler<ReceiveWebhookCommand, WebhookOutcome>
{
    /// <inheritdoc />
    public async ValueTask<WebhookOutcome> Handle(ReceiveWebhookCommand command, CancellationToken ct)
    {
        var check = await db.Checks.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == command.Slug, ct);

        // Unknown check, uninstalled plugin and undeclared webhook all look the same: the URL does not exist.
        if (check is null
            || !registry.TryGetCheck(check.Plugin, out var plugin)
            || !plugin.Webhooks.TryGetValue(command.Webhook, out var handler))
        {
            throw new NotFoundException("webhook", $"{command.Slug}/{command.Webhook}");
        }

        if (!WebhookTokens.Matches(command.Token, check.WebhookTokenHash))
        {
            throw new UnauthorizedException("Missing or invalid webhook token.");
        }

        if (!check.Enabled)
        {
            throw new ForbiddenException($"Check '{check.Slug}' is disabled.");
        }

        var config = ConfigBinder.Bind(plugin, JsonDocument.Parse(check.ConfigJson).RootElement, "config").Value;
        var context = new WebhookContext(check.Id, config, sink, time);

        var outcome = await handler.HandleAsync(command.Request, context, ct);
        if (outcome == WebhookOutcome.Accepted)
        {
            await receipts.RecordAsync(check.Id, command.Webhook, time.GetUtcNow(), ct);
        }

        return outcome;
    }
}
