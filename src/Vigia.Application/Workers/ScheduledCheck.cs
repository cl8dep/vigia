namespace Vigia.Application.Workers;

/// <summary>
/// Scheduling state of one assignment inside <see cref="WorkerScheduler"/>.
/// </summary>
/// <param name="assignment">Current assignment.</param>
public sealed class ScheduledCheck(CheckAssignment assignment)
{
    private int _running;

    /// <summary>Latest assignment; replaced when the check changes.</summary>
    public CheckAssignment Assignment { get; set; } = assignment;

    /// <summary>
    /// When the next probe is due. Queue entries with a different time are stale and ignored,
    /// which is how rescheduling works without removing items from the priority queue.
    /// </summary>
    public DateTimeOffset NextRun { get; set; }

    /// <summary>Marks the check as running. False if a probe is already in flight.</summary>
    public bool TryStart()
    {
        return Interlocked.CompareExchange(ref _running, 1, 0) == 0;
    }

    /// <summary>Marks the in-flight probe as finished.</summary>
    public void Finish()
    {
        Volatile.Write(ref _running, 0);
    }
}
