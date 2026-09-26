namespace Vigia.Infrastructure.Results;

/// <summary>
/// What one run of <see cref="ResultMaintenance"/> did.
/// </summary>
/// <param name="RolledUpFrom">Start of the recomputed window, or null if there was nothing to roll up.</param>
/// <param name="RollupsWritten">Rollup rows inserted or updated.</param>
/// <param name="ResultsDeleted">Raw results deleted by retention.</param>
/// <param name="RollupsDeleted">Rollups deleted by retention.</param>
public sealed record MaintenanceReport(DateTimeOffset? RolledUpFrom, int RollupsWritten, int ResultsDeleted, int RollupsDeleted);
