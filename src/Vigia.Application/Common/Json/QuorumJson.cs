using System.Text.Json;
using Vigia.Application.Common.Exceptions;
using Vigia.Domain.Checks;
using Vigia.Domain.Common;

namespace Vigia.Application.Common.Json;

/// <summary>
/// Reads a quorum as clients write it: <c>2</c>, <c>"2"</c>, <c>"50%"</c> or <c>"majority"</c>.
/// </summary>
public static class QuorumJson
{
    /// <summary>Parses a quorum; null or absent means "not set".</summary>
    /// <exception cref="ValidationException">The value is not a valid quorum.</exception>
    public static Quorum? Parse(JsonElement? quorum, string field)
    {
        if (quorum is null || quorum.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var text = quorum.Value.ValueKind switch
        {
            JsonValueKind.Number => quorum.Value.GetRawText(),
            JsonValueKind.String => quorum.Value.GetString()!,
            _ => throw new ValidationException(field, "Use a count such as 2, a percentage such as \"50%\", or \"majority\"."),
        };

        try
        {
            return Quorum.Parse(text);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(field, ex.Message);
        }
    }
}
