using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Vigia.Application.Agents;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Agents;

/// <summary>
/// Scheduling behavior of <see cref="AgentScheduler"/> with simulated time.
/// </summary>
public sealed class AgentSchedulerTests : IAsyncDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly FakeAssignmentSource _source = new();
    private readonly GatedProbeExecutor _executor = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly ServiceProvider _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
    private Task _run = Task.CompletedTask;

    [Fact]
    public async Task Probes_each_check_on_its_interval()
    {
        var check = Assignment("a");
        _source.Set(check);
        Start();

        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 1);
        _time.Advance(TimeSpan.FromSeconds(5));
        await Eventually.StillTrueAsync(() => _executor.Started(check.CheckId) == 1);
        _time.Advance(TimeSpan.FromSeconds(5));
        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 2);
        _time.Advance(Interval);
        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 3);
    }

    [Fact]
    public async Task Coinciding_checks_never_exceed_max_concurrency()
    {
        var checks = Enumerable.Range(0, 5).Select(i => Assignment($"c{i}")).ToArray();
        _source.Set(checks);
        _executor.Close();
        Start(maxConcurrency: 2);

        await Eventually.TrueAsync(() => _executor.Running == 2);
        await Eventually.StillTrueAsync(() => _executor.TotalStarted == 2);

        _executor.Open();
        await Eventually.TrueAsync(() => checks.All(c => _executor.Started(c.CheckId) == 1));
        Assert.Equal(2, _executor.MaxRunning);
    }

    [Fact]
    public async Task Slow_probe_skips_ticks_instead_of_piling_up()
    {
        var check = Assignment("slow");
        _source.Set(check);
        _executor.Close();
        using var skipped = new MetricCollector<long>(_services.GetRequiredService<IMeterFactory>(), AgentMetrics.MeterName, "vigia.agent.probes.skipped");
        Start();

        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 1);
        _time.Advance(Interval);
        _time.Advance(Interval);
        await Eventually.StillTrueAsync(() => _executor.Started(check.CheckId) == 1);
        await Eventually.TrueAsync(() => skipped.GetMeasurementSnapshot().EvaluateAsCounter() >= 2);

        // The in-flight probe finishes asynchronously; keep ticking until the scheduler sees it free.
        _executor.Open();
        await Eventually.TrueAsync(() =>
        {
            _time.Advance(Interval);
            return _executor.Started(check.CheckId) == 2;
        });
    }

    [Fact]
    public async Task Refresh_stops_removed_checks_and_starts_new_ones()
    {
        var removed = Assignment("removed");
        var added = Assignment("added");
        _source.Set(removed);
        Start(refresh: Interval);

        await Eventually.TrueAsync(() => _executor.Started(removed.CheckId) == 1);
        _source.Set(added);
        _time.Advance(Interval);

        await Eventually.TrueAsync(() => _executor.Started(added.CheckId) == 1);
        await Eventually.StillTrueAsync(() => _executor.Started(removed.CheckId) == 1);
    }

    [Fact]
    public async Task Keeps_probing_when_the_source_fails()
    {
        var check = Assignment("resilient");
        _source.Set(check);
        Start(refresh: Interval);

        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 1);
        _source.Fail = true;
        _time.Advance(Interval);
        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 2);
        _time.Advance(Interval);
        await Eventually.TrueAsync(() => _executor.Started(check.CheckId) == 3);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _executor.Open();
        await _stop.CancelAsync();
        await _run;
        _stop.Dispose();
        await _services.DisposeAsync();
    }

    private static CheckAssignment Assignment(string slug)
    {
        return new CheckAssignment(Guid.CreateVersion7(), slug, "test", "{}", Interval, DateTimeOffset.UnixEpoch);
    }

    private void Start(int maxConcurrency = 50, TimeSpan? refresh = null)
    {
        var options = Options.Create(new AgentOptions
        {
            MaxConcurrency = maxConcurrency,
            RefreshInterval = refresh ?? TimeSpan.FromHours(1),
            InitialJitter = TimeSpan.Zero,
        });
        var metrics = new AgentMetrics(_services.GetRequiredService<IMeterFactory>());
        var scheduler = new AgentScheduler(_source, _executor, new NullResultSink(), metrics, options, _time, NullLogger<AgentScheduler>.Instance);
        _run = Task.Run(() => scheduler.RunAsync(_stop.Token));
    }
}
