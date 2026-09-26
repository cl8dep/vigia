namespace Vigia.Application.Plugins.ListPlugins;

/// <summary>
/// A webhook a plugin declares.
/// </summary>
/// <param name="Name">Webhook name, used in <c>/api/v1/hooks/{check}/{name}</c>.</param>
/// <param name="Description">What calling it means.</param>
public sealed record WebhookDto(string Name, string Description);
