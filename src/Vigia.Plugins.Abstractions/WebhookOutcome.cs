namespace Vigia.Plugins;

/// <summary>
/// What a webhook handler decided about a request.
/// </summary>
public enum WebhookOutcome
{
    /// <summary>Processed. The host records the receipt and answers 204.</summary>
    Accepted,

    /// <summary>The request did not have the expected shape. The host answers 400 and records nothing.</summary>
    Rejected,
}
