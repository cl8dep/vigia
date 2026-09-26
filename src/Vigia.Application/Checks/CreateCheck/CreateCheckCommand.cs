using System.Text.Json;
using Mediator;

namespace Vigia.Application.Checks.CreateCheck;

/// <summary>
/// Creates a check.
/// </summary>
/// <param name="Slug">Stable identity, unique.</param>
/// <param name="Name">Display name. Defaults to the slug.</param>
/// <param name="Plugin">Check plugin id.</param>
/// <param name="Config">Plugin config.</param>
/// <param name="Interval">Duration string. Defaults to the plugin default.</param>
/// <param name="Labels">Labels.</param>
public sealed record CreateCheckCommand(
    string Slug,
    string? Name,
    string Plugin,
    JsonElement Config,
    string? Interval,
    IReadOnlyDictionary<string, string>? Labels) : ICommand<CheckDto>;
