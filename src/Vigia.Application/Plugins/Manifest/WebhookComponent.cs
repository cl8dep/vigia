namespace Vigia.Application.Plugins.Manifest;

/// <summary>
/// A webhook declared in the manifest. Declaring it is what grants the capability; there is no registration in code.
/// </summary>
/// <param name="Name">Lowercase kebab case; part of the URL <c>/api/v1/hooks/{check}/{name}</c>.</param>
/// <param name="Class">Full name of the <c>IWebhookHandler</c> implementation in the entry assembly.</param>
/// <param name="Description">What calling it means, shown to users next to the URL.</param>
public sealed record WebhookComponent(string Name, string Class, string Description);
