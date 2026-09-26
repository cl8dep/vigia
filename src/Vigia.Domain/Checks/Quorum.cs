using System.Globalization;
using Vigia.Domain.Common;

namespace Vigia.Domain.Checks;

/// <summary>
/// How many of a check's workers must agree before a rule fires: a count (<c>2</c>), a percentage (<c>50%</c>)
/// or <c>majority</c>.
/// </summary>
/// <param name="Kind">How workers are counted.</param>
/// <param name="Value">The count or the percentage; unused for <see cref="QuorumKind.Majority"/>.</param>
public sealed record Quorum(QuorumKind Kind, int Value)
{
    /// <summary>More than half of the workers with fresh results.</summary>
    public static readonly Quorum Majority = new(QuorumKind.Majority, 0);

    /// <summary>Parses <c>2</c>, <c>50%</c> or <c>majority</c>.</summary>
    /// <exception cref="DomainException">The text is none of those, or out of range.</exception>
    public static Quorum Parse(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Equals("majority", StringComparison.OrdinalIgnoreCase))
        {
            return Majority;
        }

        var isPercent = trimmed.EndsWith('%');
        var number = isPercent ? trimmed[..^1] : trimmed;
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 1 || (isPercent && value > 100))
        {
            throw new DomainException($"Quorum '{text}' must be a count of at least 1, a percentage from 1% to 100%, or 'majority'.");
        }

        return new Quorum(isPercent ? QuorumKind.Percent : QuorumKind.Count, value);
    }

    /// <summary>Workers that must agree out of <paramref name="available"/>. Always at least one.</summary>
    public int Required(int available)
    {
        return Kind switch
        {
            QuorumKind.Count => Value,
            QuorumKind.Percent => Math.Max(1, (int)Math.Ceiling(available * Value / 100.0)),
            _ => (available / 2) + 1,
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Kind switch
        {
            QuorumKind.Count => Value.ToString(CultureInfo.InvariantCulture),
            QuorumKind.Percent => $"{Value}%",
            _ => "majority",
        };
    }
}
