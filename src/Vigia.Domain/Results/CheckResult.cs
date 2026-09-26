namespace Vigia.Domain.Results;

/// <summary>
/// One probe of one check by one worker. Immutable once written.
/// </summary>
public sealed class CheckResult
{
    private CheckResult()
    {
    }

    /// <summary>Creates a result.</summary>
    /// <param name="id">UUIDv7 generated where the probe ran, so re-sending the same result is idempotent.</param>
    /// <param name="checkId">Check that was probed.</param>
    /// <param name="worker">Worker that ran the probe.</param>
    /// <param name="outcome">Probe outcome.</param>
    /// <param name="measurements">Dimension name to value.</param>
    /// <param name="message">Why the target is down or the probe failed.</param>
    /// <param name="durationMs">How long the probe took.</param>
    /// <param name="observedAt">When the probe started (UTC).</param>
    public CheckResult(
        Guid id,
        Guid checkId,
        string worker,
        ResultOutcome outcome,
        IReadOnlyDictionary<string, double> measurements,
        string? message,
        double durationMs,
        DateTimeOffset observedAt)
    {
        Id = id;
        CheckId = checkId;
        Worker = worker;
        Outcome = outcome;
        Measurements = new Dictionary<string, double>(measurements);
        Message = message;
        DurationMs = durationMs;
        ObservedAt = observedAt;
    }

    public Guid Id { get; private set; }

    /// <summary>Check that was probed.</summary>
    public Guid CheckId { get; private set; }

    /// <summary>Worker that ran the probe, for example <c>builtin</c>.</summary>
    public string Worker { get; private set; } = string.Empty;

    /// <summary>Probe outcome.</summary>
    public ResultOutcome Outcome { get; private set; }

    /// <summary>Dimension name to measured value.</summary>
    public Dictionary<string, double> Measurements { get; private set; } = [];

    /// <summary>Why the target is down or the probe failed.</summary>
    public string? Message { get; private set; }

    /// <summary>How long the probe took, in milliseconds.</summary>
    public double DurationMs { get; private set; }

    /// <summary>When the probe started (UTC). Exact, never truncated.</summary>
    public DateTimeOffset ObservedAt { get; private set; }
}
