using Vigia.Application.Plugins;
using Vigia.Application.Workers;
using Vigia.Domain.Checks;
using Vigia.Domain.Workers;

namespace Vigia.Application.Placement;

/// <summary>
/// Decides which workers run which checks.
/// </summary>
public static class CheckPlacement
{
    /// <summary>
    /// Plugins that declare webhooks read receipts stored on the control plane, so their checks only run on the
    /// built-in worker.
    /// </summary>
    public static bool IsControlPlaneOnly(CheckPlugin? plugin)
    {
        return plugin is { Webhooks.Count: > 0 };
    }

    /// <summary>Whether <paramref name="worker"/> should run <paramref name="check"/>.</summary>
    public static bool CanRun(Check check, CheckPlugin? plugin, Worker worker)
    {
        return IsControlPlaneOnly(plugin) ? worker.BuiltIn : check.WorkerSelector.Matches(worker.Tags);
    }

    /// <summary>Where <paramref name="check"/> can run among <paramref name="workers"/>.</summary>
    public static PlacementDto Evaluate(Check check, CheckPlugin? plugin, IReadOnlyList<Worker> workers, DateTimeOffset now)
    {
        var eligible = workers.Where(w => CanRun(check, plugin, w)).OrderBy(w => w.Slug).ToList();
        var online = eligible.Where(w => w.LastSeenAt >= now - WorkerDto.OnlineWindow).ToList();
        var state = eligible.Count == 0 ? "unschedulable" : online.Count == 0 ? "workers-offline" : "ok";
        return new PlacementDto(state, [.. eligible.Select(w => w.Slug)], [.. online.Select(w => w.Slug)]);
    }
}
