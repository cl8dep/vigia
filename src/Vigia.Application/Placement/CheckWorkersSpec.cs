using System.Text.Json;

namespace Vigia.Application.Placement;

/// <summary>
/// Which workers run a check, as clients write it: <c>{ "match": { "region": ["eu", "us"] }, "quorum": 2 }</c>.
/// </summary>
/// <param name="Match">Tag selector over worker tags; omitted means every worker.</param>
/// <param name="Quorum">A count (<c>2</c> or <c>"2"</c>) or a percentage (<c>"50%"</c>); omitted uses the global default.</param>
public sealed record CheckWorkersSpec(IReadOnlyDictionary<string, JsonElement>? Match, JsonElement? Quorum);
