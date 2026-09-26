using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Vigia.Infrastructure.Persistence.Conversions;

/// <summary>
/// Change tracking for dictionaries by content, not by reference.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public sealed class DictionaryComparer<TValue> : ValueComparer<Dictionary<string, TValue>>
{
    /// <summary>Creates the comparer.</summary>
    public DictionaryComparer()
        : base((a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
               v => v.OrderBy(p => p.Key).Aggregate(0, (h, p) => HashCode.Combine(h, p.Key, p.Value)),
               v => new Dictionary<string, TValue>(v))
    {
    }
}
