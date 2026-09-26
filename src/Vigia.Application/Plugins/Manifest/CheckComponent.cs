namespace Vigia.Application.Plugins.Manifest;

/// <summary>
/// The <c>check</c> section of a manifest: the class the host instantiates and what it measures.
/// </summary>
/// <param name="Class">Full name of the <c>ICheck</c> implementation in the entry assembly.</param>
/// <param name="Config">Full name of the config record in the entry assembly.</param>
/// <param name="DefaultInterval">Interval for checks that do not set one. Defaults to one minute.</param>
/// <param name="Dimensions">Measurements the check may report.</param>
public sealed record CheckComponent(string Class, string Config, TimeSpan? DefaultInterval, IReadOnlyList<DimensionDeclaration>? Dimensions);
