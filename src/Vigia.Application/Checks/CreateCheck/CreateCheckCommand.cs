using System.Text.Json;
using Mediator;
using Vigia.Application.Placement;

namespace Vigia.Application.Checks.CreateCheck;

/// <summary>
/// Creates a check.
/// </summary>
/// <param name="Slug">Stable identity, unique.</param>
/// <param name="Name">Display name. Defaults to the slug.</param>
/// <param name="Plugin">Check plugin id.</param>
/// <param name="Config">Plugin config.</param>
/// <param name="Interval">Duration string. Defaults to the plugin default.</param>
/// <param name="Tags">User tags (<c>vigia:*</c> is reserved). Null value means a flag.</param>
/// <param name="Workers">Which workers run the check and the quorum; omitted means every worker.</param>
public sealed record CreateCheckCommand(
    string Slug,
    string? Name,
    string Plugin,
    JsonElement Config,
    string? Interval,
    IReadOnlyDictionary<string, string?>? Tags,
    CheckWorkersSpec? Workers = null) : ICommand<CheckDto>;
