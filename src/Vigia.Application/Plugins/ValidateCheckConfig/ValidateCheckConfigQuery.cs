using System.Text.Json;
using Mediator;

namespace Vigia.Application.Plugins.ValidateCheckConfig;

/// <summary>
/// Validates a check config against the installed plugin without saving anything.
/// Used by the CLI and the Terraform provider at plan time.
/// </summary>
/// <param name="Plugin">Plugin id.</param>
/// <param name="Config">Raw config.</param>
public sealed record ValidateCheckConfigQuery(string Plugin, JsonElement Config) : IQuery<JsonElement>;
