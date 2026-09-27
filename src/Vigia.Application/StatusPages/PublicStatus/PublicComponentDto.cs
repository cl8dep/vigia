namespace Vigia.Application.StatusPages.PublicStatus;

/// <summary>
/// A component on the public page. Only public names and states; no internal reasons, checks or workers.
/// </summary>
/// <param name="Name">Public name.</param>
/// <param name="Group">Public group, or null.</param>
/// <param name="State">Current state.</param>
/// <param name="Since">When the current state started.</param>
/// <param name="Partitions">State per partition (for example per region), when the service is partitioned.</param>
/// <param name="Uptime">Availability over the history window, 0 to 1, or null without data.</param>
/// <param name="History">One entry per day, oldest first.</param>
public sealed record PublicComponentDto(
    string Name,
    string? Group,
    string State,
    DateTimeOffset? Since,
    IReadOnlyDictionary<string, string> Partitions,
    double? Uptime,
    IReadOnlyList<PublicDayDto> History);
