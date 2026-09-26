using System.Text.Json.Nodes;

namespace Vigia.Application.Placement;

/// <summary>
/// Which workers run a check, in the shape clients write it.
/// </summary>
/// <param name="Match">Tag selector over worker tags; empty means every worker.</param>
/// <param name="Quorum">Count or percentage, or null for the global default.</param>
public sealed record CheckWorkersDto(IReadOnlyDictionary<string, JsonNode?> Match, string? Quorum);
