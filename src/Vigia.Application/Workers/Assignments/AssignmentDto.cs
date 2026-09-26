using System.Text.Json;

namespace Vigia.Application.Workers.Assignments;

/// <summary>
/// One check a remote worker must run, with everything needed to run it while disconnected.
/// </summary>
/// <param name="CheckId">Check id; results are reported against it.</param>
/// <param name="Slug">Check slug, for logs.</param>
/// <param name="Plugin">Plugin id.</param>
/// <param name="PluginVersion">Plugin version the config was validated against.</param>
/// <param name="Config">Plugin config, secrets included (the worker needs them to probe).</param>
/// <param name="Interval">Duration string.</param>
/// <param name="Version">Changes whenever the check changes.</param>
public sealed record AssignmentDto(
    Guid CheckId,
    string Slug,
    string Plugin,
    string PluginVersion,
    JsonElement Config,
    string Interval,
    DateTimeOffset Version);
