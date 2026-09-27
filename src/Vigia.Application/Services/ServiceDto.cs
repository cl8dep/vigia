using System.Text.Json.Nodes;

namespace Vigia.Application.Services;

/// <summary>
/// A service as exposed by the API.
/// </summary>
/// <param name="Slug">Stable identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Tags">Tags, including system tags.</param>
/// <param name="Checks">Tag selector over checks, or null when the service has no checks.</param>
/// <param name="PartitionBy">Partition key, or null.</param>
/// <param name="DependsOn">Dependencies.</param>
/// <param name="MatchedChecks">Slugs of the checks the selector currently matches.</param>
/// <param name="UncoveredChecks">Matched checks that no enabled rule targets, so they can never affect health.</param>
/// <param name="ManagedBy">Owning front end.</param>
public sealed record ServiceDto(
    string Slug,
    string Name,
    IReadOnlyDictionary<string, string?> Tags,
    IReadOnlyDictionary<string, JsonNode?>? Checks,
    string? PartitionBy,
    IReadOnlyList<DependencyDto> DependsOn,
    IReadOnlyList<string> MatchedChecks,
    IReadOnlyList<string> UncoveredChecks,
    string ManagedBy);
