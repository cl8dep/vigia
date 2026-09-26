# Plugin model

Inspired by Piro (`src/Piro.Checks.Abstractions`, `src/Piro.Contracts/Attributes`, `src/Piro.Integrations.Abstractions`). The core knows nothing about specific checks or integrations. It only knows plugin kinds.

## Idea

Like Android's `AndroidManifest.xml`, every plugin ships a `plugin.json` that is the **only** source of truth: identity, display data, the classes the host instantiates, and the capabilities they get. Classes contain behavior only.

- The host reads and validates the manifest before loading any code.
- Declared classes are looked up by full name **in the plugin's own assembly only** and instantiated by the host. Nothing undeclared is ever created.
- Declaring a component is what grants its capability. A webhook exists because the manifest lists it with its handler class; there is no registration in code.
- A plugin whose manifest does not match its assembly fails to load and shows up in `/api/v1/plugins/failures`; the process keeps running.

From the config record the host generates one JSON Schema, and everything else derives from it:

- Admin UI forms (no per-plugin UI code).
- YAML validation and editor autocomplete.
- API validation.
- Secret handling (encrypted at rest, never returned by the API).

## Plugin kinds

| Kind | Runs on | Does |
|---|---|---|
| `Check` | Worker (or control plane in-process) | Probes a target, returns outcome + dimensions |
| `Source` | Control plane | Receives inbound webhooks, normalizes to alerts |
| `Notifier` | Control plane, and workers in degraded mode | Delivers personal or channel notifications |
| `ContextProvider` | Control plane | Fetches context for investigation (deploys, logs, metrics). Exposed as an MCP tool |
| `AiBackend` | Control plane | LLM provider (Ollama, OpenAI-compatible, Anthropic) |
| `Publisher` | Control plane | Publishes the static status page (S3, Cloudflare Pages, folder) |
| `Action` | Control plane | Performs a change (silence, rollback). Always requires approval. Later phase |

An integration (for example GitHub) is a bundle that can provide several kinds at once: a `Source` for deploy webhooks, a `ContextProvider` for commits, an `Action` for reverting.

## Manifest

```json
{
  "id": "vigia.check.heartbeat",
  "version": "1.0.0",
  "sdk": "1.x",
  "entry": "Vigia.Check.Heartbeat.dll",
  "label": "Heartbeat",
  "description": "A job pings Vigia when it runs; a missed ping marks the check down.",
  "check": {
    "class": "Vigia.Check.Heartbeat.HeartbeatCheck",
    "config": "Vigia.Check.Heartbeat.HeartbeatCheckConfig",
    "defaultInterval": "1m",
    "dimensions": [{ "name": "since-last-ping", "direction": "higherIsWorse", "unit": "s" }]
  },
  "webhooks": [
    { "name": "ping", "class": "Vigia.Check.Heartbeat.PingHandler", "description": "Call when the job runs successfully." }
  ]
}
```

The format is described by [`schemas/plugin.schema.json`](../schemas/plugin.schema.json) (JSON Schema 2020-12). Point `"$schema"` at it for editor autocomplete and validation; the host ignores that field. Tests keep the schema and the parser in sync.

Validated at load:

| Rule | Failure |
|---|---|
| Unknown or missing fields | Invalid manifest |
| `id` / `version` match the folder `<id>/<version>` | Rejected |
| `check.class` exists in the entry assembly, implements `ICheck`, has a public parameterless constructor | Rejected |
| `check.config` exists and equals `TConfig` when the class derives `Check<TConfig>` | Rejected |
| `webhooks[].class` exists, implements `IWebhookHandler`, has a parameterless constructor | Rejected |
| Webhook and dimension names are lowercase kebab case and unique | Rejected |
| A probe reports a measurement whose dimension is not declared | That probe is an `error` result |
| A probe asks for `IWebhookReceipts` without declaring webhooks | That probe is an `error` result |

## Code

```csharp
public sealed record HeartbeatCheckConfig
{
    [Field("Expected every")]
    public TimeSpan Every { get; init; } = TimeSpan.FromMinutes(5);
}

public sealed class HeartbeatCheck : Check<HeartbeatCheckConfig>
{
    public override async Task<ProbeResult> ProbeAsync(HeartbeatCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        var last = await ctx.GetRequiredService<IWebhookReceipts>().LastReceivedAsync("ping", ct);
        // ...
        return ProbeResult.Up(new Measurement("since-last-ping", seconds));
    }
}

public sealed class PingHandler : IWebhookHandler
{
    public async Task<WebhookOutcome> HandleAsync(WebhookRequest request, IWebhookContext context, CancellationToken ct)
    {
        await context.RecordResultAsync(ProbeResult.Up(), ct);
        return WebhookOutcome.Accepted;
    }
}
```

## Webhooks

- URL: `/api/v1/hooks/{check-slug}/{webhook}`, `GET` or `POST`.
- Authenticated by the host with a per-check token (`?token=` or `X-Vigia-Token`). Only the SHA-256 hash is stored; the token is shown once on create and can be rotated. The plugin never sees it.
- When the handler returns `Accepted`, the host records a receipt (`webhook_receipts`), separate from results, so a probe reading receipts never mistakes its own results for received webhooks.
- Checks whose plugin reads receipts run on the control plane, where receipts live.

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

Checks declare the measurements they produce in `check.dimensions` (name, direction, unit), so alert rules are generic and the core never knows plugin-specific names:

```json
"dimensions": [
  { "name": "latency", "direction": "higherIsWorse", "unit": "ms" },
  { "name": "days-to-expiry", "direction": "lowerIsWorse", "unit": "d" }
]
```

Code reports values by name: `new Measurement("days-to-expiry", 12.5)`. Alert rules reference dimensions by name, so a new check type gets alerting for free.

## Labels (entity attributes)

Separate from C# attributes: every entity (service, check, worker, alert, user) carries `key=value` labels. Labels drive:

- Which workers run a check (`network=flystern-vpc`).
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

Planned manifest additions: `sha256` / signature for distribution to workers, and `runsOn` for plugins that must stay on the control plane.

### Isolation

- One `AssemblyLoadContext` per plugin. Each plugin brings its own dependencies without clashing with other plugins or the host.
- `Vigia.Plugins.Abstractions` (the SDK) is always resolved from the host context, so contract types are shared. This is the one assembly a plugin must not ship.
- Load contexts are not collectible. An unreferenced collectible context starts unloading when the GC finalizes it, which broke lazily loaded plugin dependencies (found by the DNS plugin tests). Hot reload, if ever needed, must keep a strong reference to each context and unload explicitly.
- Plugins run in-process and are trusted code. A plugin that throws is caught per probe; a plugin that hangs is bounded by a per-probe timeout. Untrusted user code goes through a dedicated sandboxed plugin (script check), never as an assembly.

### Discovery

At startup the host, per plugin folder:

1. Reads `plugin.json`, checks `sdk` compatibility and hash.
2. Loads the entry assembly in its own context.
3. Instantiates the classes the manifest declares, validating each one (see the table above).
4. Builds the config schema from the declared config record.
5. Registers the plugin in the registry by `id`.

A plugin that fails any step is disabled and reported in the UI, never crashes the host.

### Distribution to workers

Plugins are installed once, on the control plane. Workers do not need a manual install:

1. A check assignment carries `plugin id + version + sha256`.
2. If the worker does not have it, it downloads the package from the control plane.
3. The worker verifies the hash (and signature, later) before loading.
4. Plugins are cached on disk, so degraded mode keeps working after a restart.

Workers can restrict what they accept with an allowlist, for workers in sensitive networks.

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

Loading assemblies at runtime rules out Native AOT and trimming for both the control plane and workers: AOT cannot load assemblies, and trimming can remove framework types a plugin needs. Decision:

- Framework-dependent, untrimmed, on a chiseled .NET runtime image.
- Schemas are built by reflection at load time; no source generator needed.
- Worker image size is dominated by the runtime (~100 MB). Acceptable for a Raspberry Pi; revisit only if it is a real problem.
