using Vigia.Application.Common.Exceptions;

namespace Vigia.Application.Services;

/// <summary>
/// Cycle detection over the service dependency graph.
/// </summary>
public static class DependencyGraph
{
    /// <summary>
    /// Rejects the new edges of <paramref name="service"/> if any closes a cycle with the rest of the graph.
    /// </summary>
    /// <param name="service">Service whose dependencies are being replaced.</param>
    /// <param name="dependsOn">Its new dependencies.</param>
    /// <param name="otherEdges">Every other edge in the graph (from, to).</param>
    /// <param name="slugs">Service id to slug, for the error message.</param>
    /// <exception cref="ValidationException">A cycle would be created; the message shows it.</exception>
    public static void EnsureAcyclic(
        Guid service, IEnumerable<Guid> dependsOn, IEnumerable<(Guid From, Guid To)> otherEdges, IReadOnlyDictionary<Guid, string> slugs)
    {
        var edges = otherEdges.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.Select(e => e.To).ToList());
        foreach (var target in dependsOn)
        {
            var path = FindPath(target, service, edges);
            if (path is not null)
            {
                var cycle = string.Join(" -> ", new[] { service }.Concat(path).Select(id => slugs.GetValueOrDefault(id, id.ToString())));
                throw new ValidationException("dependsOn", $"This dependency would create a cycle: {cycle}.");
            }
        }
    }

    private static List<Guid>? FindPath(Guid from, Guid to, IReadOnlyDictionary<Guid, List<Guid>> edges)
    {
        var previous = new Dictionary<Guid, Guid?> { [from] = null };
        var queue = new Queue<Guid>([from]);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == to)
            {
                var path = new List<Guid>();
                for (Guid? step = current; step is not null; step = previous[step.Value])
                {
                    path.Add(step.Value);
                }

                path.Reverse();
                return path;
            }

            foreach (var next in edges.GetValueOrDefault(current, []))
            {
                if (previous.TryAdd(next, current))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }
}
