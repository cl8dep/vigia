using Mediator;

namespace Vigia.Application.Checks.RotateWebhookToken;

/// <summary>
/// Issues a new webhook token for a check, invalidating the previous one.
/// </summary>
/// <param name="Slug">Check slug.</param>
public sealed record RotateWebhookTokenCommand(string Slug) : ICommand<WebhookTokenDto>;
