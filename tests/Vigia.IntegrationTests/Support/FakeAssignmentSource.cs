using Vigia.Application.Workers;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Assignment source whose contents tests change at will, or make fail.
/// </summary>
public sealed class FakeAssignmentSource : IAssignmentSource
{
    private readonly Lock _lock = new();
    private List<CheckAssignment> _assignments = [];

    /// <summary>When true, <see cref="GetAsync"/> throws, like an unreachable control plane.</summary>
    public bool Fail { get; set; }

    /// <summary>Replaces the assignments returned from now on.</summary>
    public void Set(params CheckAssignment[] assignments)
    {
        lock (_lock)
        {
            _assignments = [.. assignments];
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CheckAssignment>> GetAsync(CancellationToken ct)
    {
        if (Fail)
        {
            throw new HttpRequestException("Control plane unreachable.");
        }

        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CheckAssignment>>([.. _assignments]);
        }
    }
}
