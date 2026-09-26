# Plugin model

Inspired by Piro (`src/Piro.Checks.Abstractions`, `src/Piro.Contracts/Attributes`, `src/Piro.Integrations.Abstractions`). The core knows nothing about specific checks or integrations. It only knows plugin kinds.

## Idea

A plugin is a class with:

1. **An id** (`http`, `dns`, `telegram`).
2. **A manifest**: label, description, icon, capabilities, defaults.
3. **A typed config record** annotated with attributes.

From the config record the core generates one JSON Schema, and everything else derives from it:

- Admin UI forms (no per-plugin UI code).
- YAML validation and editor autocomplete.
- API validation.
- Secret handling (encrypted at rest, never returned by the API).

## Plugin kinds

| Kind | Runs on | Does |
|---|---|---|
| `Check` | Agent (or control plane in-process) | Probes a target, returns outcome + dimensions |
| `Source` | Control plane | Receives inbound webhooks, normalizes to alerts |
| `Notifier` | Control plane, and agents in degraded mode | Delivers personal or channel notifications |
| `ContextProvider` | Control plane | Fetches context for investigation (deploys, logs, metrics). Exposed as an MCP tool |
| `AiBackend` | Control plane | LLM provider (Ollama, OpenAI-compatible, Anthropic) |
| `Publisher` | Control plane | Publishes the static status page (S3, Cloudflare Pages, folder) |
| `Action` | Control plane | Performs a change (silence, rollback). Always requires approval. Later phase |

An integration (for example GitHub) is a bundle that can provide several kinds at once: a `Source` for deploy webhooks, a `ContextProvider` for commits, an `Action` for reverting.

## Sketch

```csharp
public interface ICheck
{
    string Id { get; }
    CheckManifest Manifest { get; }
    Task<ProbeResult> ProbeAsync(object config, ICheckContext ctx, CancellationToken ct);
}

public abstract class Check<TConfig> : ICheck where TConfig : class
{
    public abstract string Id { get; }
    public abstract CheckManifest Manifest { get; }
    public abstract Task<ProbeResult> ProbeAsync(TConfig config, ICheckContext ctx, CancellationToken ct);
    Task<ProbeResult> ICheck.ProbeAsync(object config, ICheckContext ctx, CancellationToken ct) =>
        ProbeAsync((TConfig)config, ctx, ct);
}

public record DnsCheckConfig
{
    [Field("Host", Placeholder = "example.com"), Required, Validate("hostname")]
    public string Host { get; init; } = "";

    [Field("Record type"), Options("A", "AAAA", "CNAME", "MX", "TXT", "NS")]
    public string RecordType { get; init; } = "A";

    [Field("Resolvers", Help = "Empty = system resolver. Several = compare answers.")]
    public List<string>? Resolvers { get; init; }

    [Field("Expected value"), VisibleWhen(nameof(RecordType), "A", "AAAA", "CNAME", "TXT")]
    public string? Expected { get; init; }
}
```

## Attributes (config fields)

Starting set, taken from Piro:

| Attribute | Purpose |
|---|---|
| `Field` | Label, placeholder, help text |
| `Required`, `Validate` | Validation, shared between UI, API and YAML |
| `Options` | Static enum values |
| `DynamicOptions` | Options loaded at runtime (Slack channels, Jira projects) |
| `VisibleWhen` | Conditional fields |
| `Secret` | Encrypted at rest, write-only in API |
| `Multiline`, `Code`, `Markdown` | Editor hints |

## Dimensions

Checks declare the measurements they produce so alert rules are generic:

```csharp
new DimensionSpec("latency", Comparison.Threshold, Direction.HigherIsWorse, Unit: "ms")
new DimensionSpec("days_to_expiry", Comparison.Threshold, Direction.LowerIsWorse, Unit: "d")
```

Alert rules reference dimensions by name, so a new check type gets alerting for free.

## Labels (entity attributes)

Separate from C# attributes: every entity (service, check, agent, alert, user) carries `key=value` labels. Labels drive:

- Which agents run a check (`network=flystern-vpc`).
- Which escalation policy an alert routes to (`team=payments`).
- Which context providers the investigation uses (`repo=flystern/api`).
- Filtering in UI and API.

## Loading: plugins are assemblies in a folder

Every plugin, including the built-in ones, is a separate assembly loaded from a folder at startup. The core ships with zero checks, sources or notifiers; the stock ones are just plugins that come in the default image.

### Layout

```
plugins/
  vigia.check.http/
    1.2.0/
      plugin.json
      Vigia.Check.Http.dll
      <private dependencies>.dll
  vigia.notifier.telegram/
    0.4.1/
      ...
```

`plugin.json`:

```json
{
  "id": "vigia.check.http",
  "version": "1.2.0",
  "sdk": "1.x",
  "entry": "Vigia.Check.Http.dll",
  "kinds": ["check"],
  "runsOn": ["agent", "control-plane"],
  "sha256": "..."
}
```

The manifest is read without loading code, so the host can list, validate and reject plugins cheaply.

### Isolation

- One `AssemblyLoadContext` per plugin. Each plugin brings its own dependencies without clashing with other plugins or the host.
- `Vigia.Plugins.Abstractions` (the SDK) is always resolved from the host context, so contract types are shared. This is the one assembly a plugin must not ship.
- Load contexts are collectible, which keeps the door open to unload / reload later. Phase 1 loads only at startup.
- Plugins run in-process and are trusted code. A plugin that throws is caught per probe; a plugin that hangs is bounded by a per-probe timeout. Untrusted user code goes through a dedicated sandboxed plugin (script check), never as an assembly.

### Discovery

At startup the host, per plugin folder:

1. Reads `plugin.json`, checks `sdk` compatibility and hash.
2. Loads the entry assembly in its own context.
3. Finds types implementing the plugin interfaces (`ICheck`, `ISource`, `INotifier`, ...).
4. Builds the config schema from the config record attributes.
5. Registers the plugin in the registry by `id`.

A plugin that fails any step is disabled and reported in the UI, never crashes the host.

### Distribution to agents

Plugins are installed once, on the control plane. Agents do not need a manual install:

1. A check assignment carries `plugin id + version + sha256`.
2. If the agent does not have it, it downloads the package from the control plane.
3. The agent verifies the hash (and signature, later) before loading.
4. Plugins are cached on disk, so degraded mode keeps working after a restart.

Agents can restrict what they accept with an allowlist, for agents in sensitive networks.

### Versioning

- **SDK**: semver. The host supports one SDK major; a plugin built for another major is rejected with a clear message.
- **Plugin**: several versions can sit side by side. A check references `plugin@major`; the host uses the latest installed minor.
- **Config migrations**: a plugin config record carries a schema version. When it changes, the plugin provides a migration from the previous version. Stored configs are migrated on load, never silently dropped.

### Authoring

- `Vigia.Plugins.Abstractions` published as a NuGet package.
- `dotnet new vigia-check` template.
- `vigia plugin pack` produces the folder + `plugin.json` + hash.
- A test harness package to run a check against a target without a control plane.

## Trimming / AOT

Loading assemblies at runtime rules out Native AOT and trimming for both the control plane and agents: AOT cannot load assemblies, and trimming can remove framework types a plugin needs. Decision:

- Framework-dependent, untrimmed, on a chiseled .NET runtime image.
- Schemas are built by reflection at load time; no source generator needed.
- Agent image size is dominated by the runtime (~100 MB). Acceptable for a Raspberry Pi; revisit only if it is a real problem.
