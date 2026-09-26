using Vigia.Plugins;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Check referenced by manifests built in tests. Always up, always reports latency.
/// </summary>
public sealed class FakeCheck : Check<FakeCheckConfig>
{
    /// <inheritdoc />
    public override Task<ProbeResult> ProbeAsync(FakeCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        return Task.FromResult(ProbeResult.Up(Measurement.Latency(1)));
    }
}
