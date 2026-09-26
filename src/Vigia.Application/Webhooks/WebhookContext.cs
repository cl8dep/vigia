using Vigia.Application.Agents;
using Vigia.Plugins;

namespace Vigia.Application.Webhooks;

/// <summary>
/// <see cref="IWebhookContext"/> for one request to one check.
/// </summary>
/// <param name="checkId">Target check.</param>
/// <param name="config">Target check's bound config.</param>
/// <param name="sink">Where recorded results go.</param>
/// <param name="time">Clock.</param>
public sealed class WebhookContext(Guid checkId, object config, IResultSink sink, TimeProvider time) : IWebhookContext
{
    /// <summary>Agent name recorded on results written by webhooks.</summary>
    public const string Agent = "webhook";

    /// <inheritdoc />
    public object Config { get; } = config;

    /// <inheritdoc />
    public TimeProvider Time { get; } = time;

    /// <inheritdoc />
    public Task RecordResultAsync(ProbeResult result, CancellationToken ct)
    {
        var now = Time.GetUtcNow();
        var record = new ProbeRecord(
            Guid.CreateVersion7(now),
            checkId,
            Agent,
            result.Outcome,
            result.Measurements.ToDictionary(m => m.Dimension, m => m.Value),
            result.Message,
            0,
            now);
        return sink.WriteAsync(record, ct);
    }
}
