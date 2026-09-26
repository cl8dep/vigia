namespace Vigia.Plugins;

/// <summary>
/// An authenticated inbound request to a plugin webhook. Plain data, no web framework types.
/// </summary>
/// <param name="Method">HTTP method.</param>
/// <param name="Query">Query string values. The auth token is removed before the plugin sees it.</param>
/// <param name="Headers">Request headers. The auth header is removed before the plugin sees it.</param>
/// <param name="Body">Request body as text (size-limited by the host).</param>
public sealed record WebhookRequest(
    string Method,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers,
    string Body);
