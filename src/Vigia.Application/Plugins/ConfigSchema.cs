namespace Vigia.Application.Plugins;

/// <summary>
/// Schema of a plugin config, built from the config record. Single source for UI forms, YAML and Terraform validation.
/// </summary>
/// <param name="Fields">Fields in declaration order.</param>
public sealed record ConfigSchema(IReadOnlyList<ConfigField> Fields);
