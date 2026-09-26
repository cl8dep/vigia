using System.Globalization;
using Vigia.Domain.Common;

namespace Vigia.Domain.Checks;

/// <summary>
/// How many of a check's workers must agree before a rule fires: a count (<c>2</c>) or a percentage (<c>50%</c>).
/// </summary>
/// <param name="Value">Count, or percentage from 1 to 100.</param>
/// <param name="IsPercent">Whether <paramref name="Value"/> is a percentage.</param>
public sealed record Quorum(int Value, bool IsPercent)
{
    /// <summary>Parses <c>2</c> or <c>50%</c>.</summary>
    /// <exception cref="DomainException">The text is not a positive count or a percentage from 1 to 100.</exception>
    public static Quorum Parse(string text)
    {
        var trimmed = text.Trim();
        var isPercent = trimmed.EndsWith('%');
        var number = isPercent ? trimmed[..^1] : trimmed;
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 1 || (isPercent && value > 100))
        {
            throw new DomainException($"Quorum '{text}' must be a count of at least 1 or a percentage from 1% to 100%.");
        }

        return new Quorum(value, isPercent);
    }

    /// <summary>Workers needed out of <paramref name="available"/>. At least one when any worker is available.</summary>
    public int Required(int available)
    {
        if (!IsPercent)
        {
            return Value;
        }

        return Math.Max(1, (int)Math.Ceiling(available * Value / 100.0));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsPercent ? $"{Value}%" : Value.ToString(CultureInfo.InvariantCulture);
    }
}
