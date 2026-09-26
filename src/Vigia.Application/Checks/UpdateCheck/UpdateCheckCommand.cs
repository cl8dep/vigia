using System.Text.Json;
using Mediator;

namespace Vigia.Application.Checks.UpdateCheck;

/// <summary>
/// Replaces a check's settings. Full replace: omitted optional fields go back to their defaults,
/// except secret config fields, which keep their stored value.
/// </summary>
/// <param name="Slug">Check to update.</param>
/// <param name="Name">Display name. Defaults to the slug.</param>
/// <param name="Plugin">Optional; if given it must match the current plugin, which cannot change.</param>
/// <param name="Config">Plugin config.</param>
/// <param name="Interval">Duration string. Defaults to the plugin default.</param>
/// <param name="Tags">User tags; omitted means none. System tags are kept.</param>
/// <param name="Enabled">Whether the check is scheduled. Defaults to true.</param>
public sealed record UpdateCheckCommand(
    string Slug,
    string? Name,
    string? Plugin,
    JsonElement Config,
    string? Interval,
    IReadOnlyDictionary<string, string?>? Tags,
    bool? Enabled) : ICommand<CheckDto>;
