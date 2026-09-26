using Vigia.Application.Plugins.Manifest;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Manifests pointing at classes in this test assembly.
/// </summary>
public static class TestManifests
{
    /// <summary>Namespace of the test doubles.</summary>
    public const string Ns = "Vigia.IntegrationTests.Support";

    /// <summary>A valid manifest for <see cref="FakeCheck"/>, optionally with a <c>ping</c> webhook and the latency dimension.</summary>
    public static PluginManifest Valid(bool withPing = false, bool withLatency = true)
    {
        return new PluginManifest(
            "test.check.fake",
            "1.0.0",
            "1.x",
            "Vigia.IntegrationTests.dll",
            "Fake",
            "Test double.",
            new CheckComponent(
                $"{Ns}.FakeCheck",
                $"{Ns}.FakeCheckConfig",
                null,
                withLatency ? [new DimensionDeclaration("latency", DimensionDirection.HigherIsWorse, "ms")] : []),
            withPing ? [new WebhookComponent("ping", $"{Ns}.NoopWebhookHandler", "Ping.")] : []);
    }
}
