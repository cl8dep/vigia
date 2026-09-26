using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Json;
using Vigia.Application.Plugins;
using Vigia.Domain.Common;
using Vigia.Domain.Rules;
using Vigia.Domain.Tags;

namespace Vigia.Application.Rules;

/// <summary>
/// Validates a <see cref="RuleSpec"/> and applies it to a rule. Returns the targeted check's slug, if any.
/// </summary>
public static class RuleSpecApplier
{
    /// <summary>Applies <paramref name="spec"/> to <paramref name="rule"/>.</summary>
    /// <exception cref="ValidationException">The spec is invalid, with the offending field.</exception>
    public static async Task ApplyAsync(Rule rule, string slug, RuleSpec spec, IAppDbContext db, IPluginRegistry registry, CancellationToken ct)
    {
        if ((spec.Check is null) == (spec.Selector is null or { Count: 0 }))
        {
            throw new ValidationException("check", "Set exactly one of 'check' or 'selector'.");
        }

        string? plugin;
        if (spec.Check is not null)
        {
            var check = await db.Checks.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == spec.Check, ct)
                ?? throw new ValidationException("check", $"Check '{spec.Check}' does not exist.");
            rule.TargetCheck(check.Id);
            plugin = check.Plugin;
        }
        else
        {
            var selector = TagSelectorJson.Parse(spec.Selector!, "selector");
            rule.TargetSelector(selector);
            plugin = selector.SingleValue(SystemTags.Plugin);
        }

        var (condition, dimension, threshold) = ParseWhen(spec.When);
        EnsureDimensionDeclared(dimension, plugin, registry);

        var severity = Severity.Warning;
        if (spec.Severity is not null && !Enum.TryParse(spec.Severity, ignoreCase: true, out severity))
        {
            throw new ValidationException("severity", "Use info, warning or critical.");
        }

        try
        {
            rule.Rename(spec.Name ?? slug);
            rule.Configure(condition, dimension, threshold, spec.For ?? 1, spec.RecoverAfter ?? 1, severity);
        }
        catch (DomainException ex)
        {
            throw new ValidationException("when", ex.Message);
        }

        if (spec.Enabled ?? true)
        {
            rule.Enable();
        }
        else
        {
            rule.Disable();
        }
    }

    private static (ConditionKind Condition, string? Dimension, double? Threshold) ParseWhen(RuleWhen? when)
    {
        if (when is null)
        {
            throw new ValidationException("when", "A condition is required.");
        }

        if (when.Outcome is not null)
        {
            if (!when.Outcome.Equals("down", StringComparison.OrdinalIgnoreCase) || when.Dimension is not null || when.Above is not null || when.Below is not null)
            {
                throw new ValidationException("when", "Use { \"outcome\": \"down\" } on its own.");
            }

            return (ConditionKind.Down, null, null);
        }

        if (when.Dimension is null || (when.Above is null) == (when.Below is null))
        {
            throw new ValidationException("when", "Use { \"dimension\": ..., \"above\": n } or { \"dimension\": ..., \"below\": n }.");
        }

        return when.Above is not null
            ? (ConditionKind.Above, when.Dimension, when.Above)
            : (ConditionKind.Below, when.Dimension, when.Below);
    }

    private static void EnsureDimensionDeclared(string? dimension, string? plugin, IPluginRegistry registry)
    {
        // Selector rules without a single vigia:plugin can span plugins; their dimension is checked per result instead.
        if (dimension is null || plugin is null || !registry.TryGetCheck(plugin, out var checkPlugin))
        {
            return;
        }

        if (checkPlugin.Dimensions.All(d => d.Name != dimension))
        {
            var declared = string.Join(", ", checkPlugin.Dimensions.Select(d => d.Name));
            throw new ValidationException("when.dimension", $"Plugin '{plugin}' does not declare '{dimension}'. Declared: {declared}.");
        }
    }
}
