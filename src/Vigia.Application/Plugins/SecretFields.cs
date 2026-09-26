using System.Text.Json;
using System.Text.Json.Nodes;

namespace Vigia.Application.Plugins;

/// <summary>
/// Helpers for config fields marked secret, which the API never returns.
/// </summary>
public static class SecretFields
{
    /// <summary>
    /// Copies secret fields missing from <paramref name="incoming"/> from <paramref name="storedJson"/>.
    /// Clients never see secrets, so an update without them means "keep the current value".
    /// </summary>
    public static JsonElement KeepMissing(JsonElement incoming, string storedJson, ConfigSchema schema)
    {
        var secrets = schema.Fields.Where(f => f.Secret).Select(f => f.Name).ToList();
        if (secrets.Count == 0 || incoming.ValueKind is not JsonValueKind.Object)
        {
            return incoming;
        }

        var merged = JsonNode.Parse(incoming.GetRawText())!.AsObject();
        var stored = JsonNode.Parse(storedJson)!.AsObject();
        foreach (var name in secrets.Where(n => !merged.ContainsKey(n) && stored.ContainsKey(n)))
        {
            merged[name] = stored[name]!.DeepClone();
        }

        return JsonSerializer.SerializeToElement(merged);
    }
}
