using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Vigia.Application.Common.Json;

namespace Vigia.Application.Plugins.Manifest;

/// <summary>
/// Contents of <c>plugin.json</c>, the single source of truth for a plugin: identity, the components the host
/// instantiates, and the capabilities they get. Read and validated before any plugin code is loaded.
/// </summary>
/// <param name="Id">Plugin id, for example <c>vigia.check.http</c>; must match the folder name.</param>
/// <param name="Version">Plugin version; must match the version folder.</param>
/// <param name="Sdk">Supported SDK major, for example <c>1.x</c>.</param>
/// <param name="Entry">Entry assembly file name.</param>
/// <param name="Label">Display name.</param>
/// <param name="Description">What the plugin does.</param>
/// <param name="Check">The check component.</param>
/// <param name="Webhooks">Webhook components.</param>
/// <param name="Schema">Path or URL of <c>schemas/plugin.schema.json</c> for editor support; ignored by the host.</param>
/// <remarks>The JSON Schema in <c>schemas/plugin.schema.json</c> mirrors these rules; tests keep both in sync.</remarks>
public sealed partial record PluginManifest(
    string Id,
    string Version,
    string Sdk,
    string Entry,
    string Label,
    string Description,
    CheckComponent Check,
    IReadOnlyList<WebhookComponent>? Webhooks,
    [property: JsonPropertyName("$schema")] string? Schema = null)
{
    /// <summary>Manifest file name inside a plugin version folder.</summary>
    public const string FileName = "plugin.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new DurationJsonConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Parses a manifest and checks that required fields are present.</summary>
    /// <exception cref="InvalidDataException">The JSON is malformed, has unknown fields, or misses required ones.</exception>
    public static PluginManifest Parse(string json)
    {
        PluginManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PluginManifest>(json, Json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid {FileName}: {ex.Message}");
        }

        var missing = new List<string>();
        Require(missing, manifest?.Id, "id");
        Require(missing, manifest?.Version, "version");
        Require(missing, manifest?.Sdk, "sdk");
        Require(missing, manifest?.Entry, "entry");
        Require(missing, manifest?.Label, "label");
        if (manifest?.Description is null)
        {
            missing.Add("description");
        }

        if (manifest?.Check is null)
        {
            missing.Add("check");
        }
        else
        {
            Require(missing, manifest.Check.Class, "check.class");
            Require(missing, manifest.Check.Config, "check.config");
        }

        if (missing.Count > 0)
        {
            throw new InvalidDataException($"{FileName} is missing {string.Join(", ", missing)}.");
        }

        if (!IdPattern().IsMatch(manifest!.Id))
        {
            throw new InvalidDataException($"{FileName} id '{manifest.Id}' must be dotted lowercase, for example acme.check.ping.");
        }

        return manifest;
    }

    private static void Require(List<string> missing, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(field);
        }
    }

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9-]*)+$")]
    private static partial Regex IdPattern();
}
