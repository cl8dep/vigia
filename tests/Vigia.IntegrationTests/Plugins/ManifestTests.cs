using Microsoft.Extensions.Options;
using Vigia.Application.Agents;
using Vigia.Application.Plugins;
using Vigia.Application.Plugins.Manifest;
using Vigia.Infrastructure.Plugins;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// plugin.json is the only source of truth: the host instantiates exactly what it declares, and nothing else.
/// </summary>
public sealed class ManifestTests
{
    private static readonly System.Reflection.Assembly Assembly = typeof(FakeCheck).Assembly;

    [Fact]
    public void Instantiates_the_declared_components()
    {
        var plugin = CheckPluginBuilder.Build(TestManifests.Valid(withPing: true), Assembly);

        Assert.IsType<FakeCheck>(plugin.Check);
        Assert.Equal(typeof(FakeCheckConfig), plugin.ConfigType);
        Assert.IsType<NoopWebhookHandler>(plugin.Webhooks["ping"]);
        Assert.Equal(TimeSpan.FromMinutes(1), plugin.DefaultInterval);
    }

    [Fact]
    public void Missing_check_class_fails()
    {
        var manifest = TestManifests.Valid() with { Check = TestManifests.Valid().Check with { Class = $"{TestManifests.Ns}.Nope" } };

        Assert.Contains("check.class", Fails(manifest));
    }

    [Fact]
    public void Check_class_must_implement_ICheck()
    {
        var manifest = TestManifests.Valid() with { Check = TestManifests.Valid().Check with { Class = $"{TestManifests.Ns}.NoopWebhookHandler" } };

        Assert.Contains("does not implement ICheck", Fails(manifest));
    }

    [Fact]
    public void Config_must_match_the_check_type_argument()
    {
        var manifest = TestManifests.Valid() with { Check = TestManifests.Valid().Check with { Config = $"{TestManifests.Ns}.OtherConfig" } };

        Assert.Contains("is Check<FakeCheckConfig> but check.config is OtherConfig", Fails(manifest));
    }

    [Fact]
    public void Webhook_class_must_exist()
    {
        var manifest = TestManifests.Valid() with { Webhooks = [new WebhookComponent("ping", $"{TestManifests.Ns}.Nope", "x")] };

        Assert.Contains("webhooks[ping].class", Fails(manifest));
    }

    [Fact]
    public void Webhook_class_must_implement_IWebhookHandler()
    {
        var manifest = TestManifests.Valid() with { Webhooks = [new WebhookComponent("ping", $"{TestManifests.Ns}.FakeCheck", "x")] };

        Assert.Contains("does not implement IWebhookHandler", Fails(manifest));
    }

    [Fact]
    public void Classes_resolve_only_inside_the_plugin_assembly()
    {
        var hostType = typeof(ProbeExecutor).FullName!;
        var manifest = TestManifests.Valid() with { Webhooks = [new WebhookComponent("ping", hostType, "x")] };

        Assert.Contains("was not found", Fails(manifest));
    }

    [Fact]
    public void Webhook_and_dimension_names_are_validated()
    {
        var badWebhook = TestManifests.Valid() with { Webhooks = [new WebhookComponent("Not Valid", $"{TestManifests.Ns}.NoopWebhookHandler", "x")] };
        var duplicated = TestManifests.Valid() with
        {
            Webhooks = [new WebhookComponent("ping", $"{TestManifests.Ns}.NoopWebhookHandler", "x"), new WebhookComponent("ping", $"{TestManifests.Ns}.NoopWebhookHandler", "y")],
        };
        var badDimension = TestManifests.Valid() with
        {
            Check = TestManifests.Valid().Check with { Dimensions = [new DimensionDeclaration("Latency MS", DimensionDirection.HigherIsWorse, null)] },
        };

        Assert.Contains("invalid webhook name", Fails(badWebhook));
        Assert.Contains("more than once", Fails(duplicated));
        Assert.Contains("invalid dimension name", Fails(badDimension));
    }

    [Fact]
    public void Parse_rejects_unknown_and_missing_fields()
    {
        var unknown = Assert.Throws<InvalidDataException>(() => PluginManifest.Parse("""
            { "id": "a", "version": "1.0.0", "sdk": "1.x", "entry": "a.dll", "label": "A", "description": "",
              "check": { "class": "A", "config": "B" }, "kinds": ["check"] }
            """));
        var missing = Assert.Throws<InvalidDataException>(() => PluginManifest.Parse("""{ "id": "a", "check": { "class": "A" } }"""));

        Assert.Contains("kinds", unknown.Message);
        Assert.Contains("version, sdk, entry, label, description, check.config", missing.Message);
    }

    [Fact]
    public async Task Reporting_an_undeclared_dimension_is_an_error()
    {
        var undeclared = await ProbeAsync(TestManifests.Valid(withLatency: false));
        var declared = await ProbeAsync(TestManifests.Valid());

        Assert.Equal(Outcome.Error, undeclared.Outcome);
        Assert.Contains("undeclared dimension(s) latency", undeclared.Message);
        Assert.Equal(Outcome.Up, declared.Outcome);
    }

    [Fact]
    public void Receipts_require_declared_webhooks()
    {
        var plugin = CheckPluginBuilder.Build(TestManifests.Valid(), Assembly);
        var context = new ProbeContext(new NoServicesContext(), Guid.NewGuid(), plugin, new NoReceiptStore());

        var error = Assert.Throws<InvalidOperationException>(() => context.GetRequiredService<IWebhookReceipts>());

        Assert.Contains("declares no webhooks", error.Message);
    }

    [Fact]
    public async Task Receipts_only_answer_for_declared_webhooks()
    {
        var plugin = CheckPluginBuilder.Build(TestManifests.Valid(withPing: true), Assembly);
        var receipts = new ProbeContext(new NoServicesContext(), Guid.NewGuid(), plugin, new NoReceiptStore()).GetRequiredService<IWebhookReceipts>();

        Assert.Null(await receipts.LastReceivedAsync("ping", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => receipts.LastReceivedAsync("other", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Receipts_are_unavailable_off_the_control_plane()
    {
        var plugin = CheckPluginBuilder.Build(TestManifests.Valid(withPing: true), Assembly);
        var context = new ProbeContext(new NoServicesContext(), Guid.NewGuid(), plugin, receipts: null);

        var error = Assert.Throws<InvalidOperationException>(() => context.GetRequiredService<IWebhookReceipts>());

        Assert.Contains("only exist on the control plane", error.Message);
    }

    private static string Fails(PluginManifest manifest)
    {
        return Assert.Throws<InvalidOperationException>(() => CheckPluginBuilder.Build(manifest, Assembly)).Message;
    }

    private static async Task<ProbeResult> ProbeAsync(PluginManifest manifest)
    {
        var plugin = CheckPluginBuilder.Build(manifest, Assembly);
        var executor = new ProbeExecutor(new PluginRegistry([plugin], []), new NoServicesContext(), Options.Create(new AgentOptions()));
        var record = await executor.ExecuteAsync(
            new CheckAssignment(Guid.NewGuid(), "fake", plugin.Id, "{}", TimeSpan.FromMinutes(1), DateTimeOffset.UnixEpoch),
            TestContext.Current.CancellationToken);
        return new ProbeResult(record.Outcome, [.. record.Measurements.Select(m => new Measurement(m.Key, m.Value))], record.Message);
    }
}
