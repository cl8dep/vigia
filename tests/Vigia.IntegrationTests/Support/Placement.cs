namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Placements used by tests that simulate a single-node install.
/// </summary>
public static class Placement
{
    /// <summary>
    /// Runs a check only on the built-in worker, so remote workers other tests left online never join its quorum.
    /// </summary>
    public static readonly object BuiltInOnly = new { match = new Dictionary<string, object?> { ["vigia:builtin"] = null } };
}
