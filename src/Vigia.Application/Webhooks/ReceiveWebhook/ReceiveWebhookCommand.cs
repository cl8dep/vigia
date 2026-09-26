using Mediator;
using Vigia.Plugins;

namespace Vigia.Application.Webhooks.ReceiveWebhook;

/// <summary>
/// An inbound request to a check webhook, before authentication.
/// </summary>
/// <param name="Slug">Target check.</param>
/// <param name="Webhook">Webhook name.</param>
/// <param name="Token">Token from the query string or header, if any.</param>
/// <param name="Request">Request data passed to the plugin (without the token).</param>
public sealed record ReceiveWebhookCommand(string Slug, string Webhook, string? Token, WebhookRequest Request) : ICommand<WebhookOutcome>;
