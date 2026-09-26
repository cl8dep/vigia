using Vigia.Application.Common.Exceptions;
using Vigia.Domain.Common;
using Vigia.Domain.Workers;

namespace Vigia.Application.Workers;

/// <summary>
/// Applies a <see cref="WorkerSpec"/> to a worker, turning domain errors into field errors.
/// </summary>
public static class WorkerSpecApplier
{
    /// <summary>Applies <paramref name="spec"/>.</summary>
    /// <exception cref="ValidationException">A field is invalid.</exception>
    public static void Apply(Worker worker, string slug, WorkerSpec spec)
    {
        Apply("name", () => worker.Rename(spec.Name ?? slug));
        Apply("region", () => worker.MoveTo(spec.Region));
        Apply("tags", () => worker.SetTags(spec.Tags ?? new Dictionary<string, string?>()));
    }

    private static void Apply(string field, Action action)
    {
        try
        {
            action();
        }
        catch (DomainException ex)
        {
            throw new ValidationException(field, ex.Message);
        }
    }
}
