using System.Globalization;
using System.Text.RegularExpressions;

namespace Vigia.Application.Common;

/// <summary>
/// Parses and formats durations the way config files write them: <c>500ms</c>, <c>30s</c>, <c>5m</c>, <c>1h</c>, <c>1d</c>.
/// </summary>
public static partial class Duration
{
    /// <summary>Parses a duration. Also accepts <see cref="TimeSpan"/> constant format (<c>00:00:30</c>).</summary>
    public static bool TryParse(string? value, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = Pattern().Match(value.Trim());
        if (!match.Success)
        {
            return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);
        }

        var amount = double.Parse(match.Groups["n"].Value, CultureInfo.InvariantCulture);
        result = match.Groups["u"].Value switch
        {
            "ms" => TimeSpan.FromMilliseconds(amount),
            "s" => TimeSpan.FromSeconds(amount),
            "m" => TimeSpan.FromMinutes(amount),
            "h" => TimeSpan.FromHours(amount),
            _ => TimeSpan.FromDays(amount),
        };
        return true;
    }

    /// <summary>Formats with the largest unit that represents the value exactly.</summary>
    public static string Format(TimeSpan value)
    {
        if (value.Ticks % TimeSpan.TicksPerDay == 0 && value.Ticks != 0)
        {
            return $"{value.TotalDays:0}d";
        }

        if (value.Ticks % TimeSpan.TicksPerHour == 0 && value.Ticks != 0)
        {
            return $"{value.TotalHours:0}h";
        }

        if (value.Ticks % TimeSpan.TicksPerMinute == 0 && value.Ticks != 0)
        {
            return $"{value.TotalMinutes:0}m";
        }

        if (value.Ticks % TimeSpan.TicksPerSecond == 0)
        {
            return $"{value.TotalSeconds:0}s";
        }

        return $"{value.TotalMilliseconds:0}ms";
    }

    [GeneratedRegex(@"^(?<n>\d+(\.\d+)?)(?<u>ms|s|m|h|d)$")]
    private static partial Regex Pattern();
}
