using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;
using Vigia.Application.Plugins.Manifest;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// Keeps <c>schemas/plugin.schema.json</c> honest: real manifests pass, broken ones fail,
/// and the schema describes exactly the fields the host parses.
/// </summary>
public sealed class ManifestSchemaTests
{
    private static readonly JsonSchema Schema = JsonSchema.FromFile(Path.Combine(RepoPaths.Root, "schemas", "plugin.schema.json"));

    public static TheoryData<string> BuiltInManifests()
    {
        return [.. Directory.GetFiles(Path.Combine(RepoPaths.Root, "plugins"), "plugin.json", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(p => Path.GetRelativePath(RepoPaths.Root, p))];
    }

    [Theory]
    [MemberData(nameof(BuiltInManifests))]
    public void Built_in_manifests_are_valid(string path)
    {
        var json = File.ReadAllText(Path.Combine(RepoPaths.Root, path));

        Assert.True(Evaluate(json).IsValid, $"{path} does not match the schema.");
        PluginManifest.Parse(json);
    }

    [Theory]
    [InlineData("kinds", """["check"]""")]
    [InlineData("id", "\"Vigia.HTTP\"")]
    [InlineData("version", "\"1.0\"")]
    [InlineData("sdk", "\"2.x\"")]
    [InlineData("entry", "\"Plugin.exe\"")]
    [InlineData("webhooks", """[{ "name": "Not Valid", "class": "A.B", "description": "x" }]""")]
    [InlineData("webhooks", """[{ "name": "ping", "description": "x" }]""")]
    [InlineData("check", """{ "class": "A.B", "config": "A.C", "dimensions": [{ "name": "latency", "direction": "up" }] }""")]
    [InlineData("check", """{ "class": "A.B", "config": "A.C", "defaultInterval": "soon" }""")]
    [InlineData("check", """{ "class": "A.B" }""")]
    public void Broken_manifests_are_rejected(string property, string value)
    {
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(RepoPaths.Root, "plugins", "Vigia.Check.Heartbeat", "plugin.json")))!.AsObject();
        manifest[property] = JsonNode.Parse(value);

        Assert.False(Evaluate(manifest.ToJsonString()).IsValid);
    }

    [Fact]
    public void Missing_description_is_rejected()
    {
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(RepoPaths.Root, "plugins", "Vigia.Check.Http", "plugin.json")))!.AsObject();
        manifest.Remove("description");

        Assert.False(Evaluate(manifest.ToJsonString()).IsValid);
        Assert.Throws<InvalidDataException>(() => PluginManifest.Parse(manifest.ToJsonString()));
    }

    [Fact]
    public void Schema_describes_exactly_the_parsed_fields()
    {
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(RepoPaths.Root, "schemas", "plugin.schema.json")))!;

        Assert.Equal(JsonNames<PluginManifest>(), Keys(schema["properties"]!));
        Assert.Equal(JsonNames<CheckComponent>(), Keys(schema["properties"]!["check"]!["properties"]!));
        Assert.Equal(JsonNames<DimensionDeclaration>(), Keys(schema["$defs"]!["dimension"]!["properties"]!));
        Assert.Equal(JsonNames<WebhookComponent>(), Keys(schema["$defs"]!["webhook"]!["properties"]!));
    }

    private static EvaluationResults Evaluate(string json)
    {
        return Schema.Evaluate(JsonDocument.Parse(json).RootElement);
    }

    private static SortedSet<string> Keys(JsonNode properties)
    {
        return [.. properties.AsObject().Select(p => p.Key)];
    }

    private static SortedSet<string> JsonNames<T>()
    {
        return [.. typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetMethod?.GetParameters().Length == 0 && p.DeclaringType == typeof(T) && p.CanWrite)
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name))];
    }
}
