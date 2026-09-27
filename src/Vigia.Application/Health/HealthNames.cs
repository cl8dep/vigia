using System.Text.Json;
using Vigia.Domain.Health;

namespace Vigia.Application.Health;

/// <summary>
/// API spelling of health states: kebab case, such as <c>partial-outage</c>.
/// </summary>
public static class HealthNames
{
    /// <summary>API name of a state.</summary>
    public static string Of(HealthState state)
    {
        return JsonNamingPolicy.KebabCaseLower.ConvertName(state.ToString());
    }

    /// <summary>Maps a stored snapshot.</summary>
    public static ServiceHealthDto ToDto(ServiceHealth health)
    {
        return new ServiceHealthDto(
            Of(health.State),
            health.Reason,
            health.Since,
            health.Partitions.ToDictionary(p => p.Key, p => Of(p.Value)),
            health.EvaluatedAt);
    }
}
