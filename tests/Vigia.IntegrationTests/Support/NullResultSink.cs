using Vigia.Application.Workers;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Result sink that discards results.
/// </summary>
public sealed class NullResultSink : IResultSink
{
    /// <inheritdoc />
    public Task WriteAsync(ProbeRecord record, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
