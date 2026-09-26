namespace Vigia.Application.Checks.RotateWebhookToken;

/// <summary>
/// A newly issued webhook token. Shown once; only its hash is stored.
/// </summary>
/// <param name="Token">Token to send as <c>?token=</c> or the <c>X-Vigia-Token</c> header.</param>
public sealed record WebhookTokenDto(string Token);
