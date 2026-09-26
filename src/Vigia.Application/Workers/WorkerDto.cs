using Vigia.Domain.Workers;

namespace Vigia.Application.Workers;

/// <summary>
/// A worker as exposed by the API. Secrets are only present in the response that creates them.
/// </summary>
/// <param name="Slug">Stable identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Region">Region, also the <c>vigia:region</c> tag.</param>
/// <param name="Tags">Tags, including system tags.</param>
/// <param name="BuiltIn">Whether it runs inside the control plane.</param>
/// <param name="Enrolled">Whether it holds a valid credential (always false for the built-in worker).</param>
/// <param name="Online">Whether it reported within <see cref="OnlineWindow"/>.</param>
/// <param name="LastSeenAt">Last report.</param>
/// <param name="Version">Worker version from the last report.</param>
/// <param name="EnrollmentToken">One-time enrollment token; only when just issued.</param>
/// <param name="EnrollmentExpiresAt">When the pending enrollment token expires.</param>
public sealed record WorkerDto(
    string Slug,
    string Name,
    string? Region,
    IReadOnlyDictionary<string, string?> Tags,
    bool BuiltIn,
    bool Enrolled,
    bool Online,
    DateTimeOffset? LastSeenAt,
    string? Version,
    string? EnrollmentToken,
    DateTimeOffset? EnrollmentExpiresAt)
{
    /// <summary>A worker is online if it reported within this window.</summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(2);

    /// <summary>Maps a worker.</summary>
    /// <param name="worker">Entity.</param>
    /// <param name="now">Current time, for <see cref="Online"/>.</param>
    /// <param name="enrollmentToken">Token to show once, if just issued.</param>
    public static WorkerDto From(Worker worker, DateTimeOffset now, string? enrollmentToken = null)
    {
        return new WorkerDto(
            worker.Slug,
            worker.Name,
            worker.Region,
            worker.Tags,
            worker.BuiltIn,
            worker.Enrolled,
            worker.LastSeenAt >= now - OnlineWindow,
            worker.LastSeenAt,
            worker.Version,
            enrollmentToken,
            worker.CanEnroll(now) ? worker.EnrollmentExpiresAt : null);
    }
}
