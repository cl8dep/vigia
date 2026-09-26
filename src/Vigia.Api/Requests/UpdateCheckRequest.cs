using System.Text.Json;
using Vigia.Application.Placement;

namespace Vigia.Api.Requests;

/// <summary>
/// Body of <c>PUT /api/v1/checks/{slug}</c>. The slug comes from the route.
/// </summary>
/// <param name="Name">Display name. Defaults to the slug.</param>
/// <param name="Plugin">Optional; must match the current plugin.</param>
/// <param name="Config">Plugin config. Secret fields may be omitted to keep their value.</param>
/// <param name="Interval">Duration string. Defaults to the plugin default.</param>
/// <param name="Tags">User tags; omitted means none. System tags are kept.</param>
/// <param name="Enabled">Whether the check is scheduled. Defaults to true.</param>
/// <param name="Workers">Which workers run the check and the quorum; omitted means every worker.</param>
public sealed record UpdateCheckRequest(
    string? Name,
    string? Plugin,
    JsonElement Config,
    string? Interval,
    IReadOnlyDictionary<string, string?>? Tags,
    bool? Enabled,
    CheckWorkersSpec? Workers);
