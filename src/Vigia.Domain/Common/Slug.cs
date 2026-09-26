using System.Text.RegularExpressions;

namespace Vigia.Domain.Common;

/// <summary>
/// Rules for slugs, the stable human identity used by config as code.
/// </summary>
public static partial class Slug
{
    /// <summary>Maximum slug length.</summary>
    public const int MaxLength = 63;

    /// <summary>Whether <paramref name="value"/> is lowercase kebab case, starts with a letter and fits <see cref="MaxLength"/>.</summary>
    public static bool IsValid(string? value)
    {
        return value is { Length: > 0 and <= MaxLength } && Pattern().IsMatch(value);
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$")]
    private static partial Regex Pattern();
}
