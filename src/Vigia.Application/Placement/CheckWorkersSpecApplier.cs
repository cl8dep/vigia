using System.Text.Json;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Json;
using Vigia.Application.Plugins;
using Vigia.Domain.Checks;
using Vigia.Domain.Common;
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

        check.PlaceOn(selector, ParseQuorum(spec?.Quorum));
    }

    /// <summary>The quorum in the shape clients write it.</summary>
    public static CheckWorkersDto ToDto(Check check)
    {
        return new CheckWorkersDto(TagSelectorJson.Write(check.WorkerSelector), check.Quorum?.ToString());
    }

    private static Quorum? ParseQuorum(JsonElement? quorum)
    {
        if (quorum is null || quorum.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var text = quorum.Value.ValueKind switch
        {
            JsonValueKind.Number => quorum.Value.GetRawText(),
            JsonValueKind.String => quorum.Value.GetString()!,
            _ => throw new ValidationException("workers.quorum", "Use a count such as 2, a percentage such as \"50%\", or \"majority\"."),
        };

        try
        {
            return Quorum.Parse(text);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("workers.quorum", ex.Message);
        }
    }
}
