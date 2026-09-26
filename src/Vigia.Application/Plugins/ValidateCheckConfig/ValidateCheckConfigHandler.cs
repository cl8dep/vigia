using System.Text.Json;
using Mediator;
using Vigia.Application.Common.Exceptions;

namespace Vigia.Application.Plugins.ValidateCheckConfig;

/// <summary>
/// Handles <see cref="ValidateCheckConfigQuery"/>. Returns the normalized config (defaults filled in).
/// </summary>
public sealed class ValidateCheckConfigHandler(IPluginRegistry registry) : IQueryHandler<ValidateCheckConfigQuery, JsonElement>
{
    /// <inheritdoc />
    public ValueTask<JsonElement> Handle(ValidateCheckConfigQuery query, CancellationToken ct)
    {
        if (!registry.TryGetCheck(query.Plugin, out var plugin))
        {
            throw new ValidationException("plugin", $"Plugin '{query.Plugin}' is not installed.");
        }

        var bound = ConfigBinder.Bind(plugin, query.Config, "config");
        return ValueTask.FromResult(bound.Normalized);
    }
}
