using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Json;
using Vigia.Application.Plugins;
using Vigia.Domain.Checks;
using Vigia.Domain.Tags;

namespace Vigia.Application.Placement;

/// <summary>
/// Validates a <see cref="CheckWorkersSpec"/> and applies it to a check.
/// </summary>
public static class CheckWorkersSpecApplier
{
    /// <summary>Applies <paramref name="spec"/> (null means every worker, default quorum).</summary>
    /// <exception cref="ValidationException">The selector or quorum is invalid, or the plugin cannot be placed.</exception>
    public static void Apply(Check check, CheckPlugin plugin, CheckWorkersSpec? spec)
    {
        var selector = spec?.Match is { Count: > 0 } match ? TagSelectorJson.Parse(match, "workers.match") : TagSelector.Any;
        if (!selector.IsEmpty && CheckPlacement.IsControlPlaneOnly(plugin))
        {
            throw new ValidationException("workers.match", $"Plugin '{plugin.Id}' receives webhooks, so its checks run on the control plane only.");
        }

        check.PlaceOn(selector, QuorumJson.Parse(spec?.Quorum, "workers.quorum"));
    }

    /// <summary>The quorum in the shape clients write it.</summary>
    public static CheckWorkersDto ToDto(Check check)
    {
        return new CheckWorkersDto(TagSelectorJson.Write(check.WorkerSelector), check.Quorum?.ToString());
    }
}
