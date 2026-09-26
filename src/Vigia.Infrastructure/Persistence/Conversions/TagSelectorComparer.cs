using Microsoft.EntityFrameworkCore.ChangeTracking;
using Vigia.Domain.Tags;

namespace Vigia.Infrastructure.Persistence.Conversions;

/// <summary>
/// Change tracking for selectors by content.
/// </summary>
public sealed class TagSelectorComparer : ValueComparer<TagSelector>
{
    /// <summary>Creates the comparer.</summary>
    public TagSelectorComparer()
        : base((a, b) => TagSelectorConverter.Serialize(a!) == TagSelectorConverter.Serialize(b!),
               v => TagSelectorConverter.Serialize(v).GetHashCode(StringComparison.Ordinal),
               v => v)
    {
    }
}
