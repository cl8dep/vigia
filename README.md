# Vigia

Self-hosted monitoring, on-call and AI-assisted incident investigation. Working name.

> **Status: early development.** The checks API, plugin system, built-in worker, result storage, rules and alerts work. Notifications, on-call, remote workers and the AI layer are designed but not built yet. See [docs/](docs/).

## What works today

- **Plugin system.** Every check type is a separate assembly loaded at startup from `plugins/<id>/<version>/`, isolated in its own load context. The config schema is generated from the plugin's config record and attributes, and drives validation for the API (and later UI, YAML and Terraform).
- **Built-in checks**, each its own plugin:

  | Plugin | Checks | Dimensions |
  |---|---|---|
  | `vigia.check.http` | Status code, optional body text | `latency`, `status-code` |
  | `vigia.check.tcp` | TCP connection accepted | `latency` |
  | `vigia.check.tls` | Handshake, chain and name validation, expiry | `latency`, `days-to-expiry` |
  | `vigia.check.dns` | Answer from one or more resolvers, each judged separately | `latency`, `failed-resolvers` |
  | `vigia.check.ping` | ICMP echo; down only on total loss | `latency`, `packet-loss` |
  | `vigia.check.heartbeat` | A job calls the `ping` webhook; down when a ping is late | `since-last-ping` |

  Ping on Linux needs `CAP_NET_RAW` or unprivileged ICMP (`net.ipv4.ping_group_range`).
- **Checks API.** Create, read, update, delete, validate config without saving, probe on demand.
- **Built-in worker.** Probes every enabled check on its interval with bounded concurrency, jitter and no overlapping probes. Emits metrics on meter `Vigia.Worker`.
- **Results.** Every probe is stored with exact timestamps. Hourly rollups keep per-dimension min / avg / max / p95. Raw results are kept 14 days, rollups 400 days.
- **Workers.** Remote workers are registered by an admin, enroll once with a one-time token and authenticate with their own credential on a separate API (`/worker/v1`). The built-in worker registers itself with `vigia:builtin` and `vigia:region`.
- **Quorum.** With several workers each keeps its own streak; a rule fires when its quorum (the rule's, else the check's) (count, percentage or `majority`, the default) of online eligible workers fail it, and says where: "(failing from eu-west, us-east: 2 of 3)".
- **Placement.** Checks pick workers with `workers.match` (a tag selector over worker tags) and set a `quorum`. Each check reports its placement: `ok`, `unschedulable` (no worker matches) or `workers-offline`. Heartbeat checks stay on the control plane.
- **Services.** Group checks by tag selector (one check can feed several services), optionally partitioned (`partitionBy: region`), with `blocking` / `soft` / `advisory` dependencies; cycles are rejected with the offending path. Each service lists its matched checks and the ones no rule covers.
- **Service health.** Derived from firing alerts (critical: down, warning: degraded), per partition (`partial-outage` when some partitions are down) and through dependencies. Stored with a transition history, recomputed in the same transaction as the alert change and reconciled every minute.
- **Tags.** `key` or `key:value`, one value per key, on checks, workers and rules. The reserved `vigia:*` namespace has a catalog, as in Piro: reconciled tags the system derives (`vigia:plugin`, `vigia:region`, `vigia:builtin`) and assignable flags users toggle inside `tags` (`vigia:external` for third-party providers).
- **Rules and alerts.** A rule targets one check or a tag selector (AND across keys, OR within a key's values, `null` for key present; `vigia:plugin` matches the plugin id) and fires on `outcome: down` or a dimension `above` / `below` a threshold, after `for` consecutive results; it resolves after `recoverAfter`. Rules are additive. One firing alert per rule and check: new results update it (message, occurrences) instead of opening another. Error results neither fire nor recover. Results and alert changes are saved in one transaction, serialized per check.
- **Auth.** Email and password with bearer and refresh tokens. Only the first user can sign up unless `Auth:OpenSignUp` is on.

## Quick start

Requirements: .NET SDK 10, Docker.

```bash
docker compose -f docker-compose.dev.yml up -d   # Postgres
dotnet build                                     # also stages built-in plugins in artifacts/plugins
dotnet run --project src/Vigia.Api               # applies migrations on startup
```

Then, against the URL printed at startup:

```bash
# First user bootstraps the instance
curl -X POST $URL/api/v1/auth/sign-up -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","password":"Str0ng!Passw0rd"}'

TOKEN=$(curl -s -X POST $URL/api/v1/auth/sign-in -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","password":"Str0ng!Passw0rd"}' | jq -r .accessToken)

curl -X POST $URL/api/v1/checks -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"slug":"example","plugin":"vigia.check.http","config":{"url":"https://example.com"},"interval":"30s"}'

curl $URL/api/v1/checks/example/results -H "Authorization: Bearer $TOKEN"
```

## API

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/auth/sign-up` | Create an account (first user only by default) |
| `POST` | `/api/v1/auth/sign-in` | Get access and refresh tokens |
| `POST` | `/api/v1/auth/refresh` | Exchange a refresh token for new tokens |
| `GET` | `/api/v1/plugins` | Installed plugins with config schemas |
| `GET` | `/api/v1/plugins/failures` | Plugin folders that failed to load |
| `GET` | `/api/v1/checks` | List checks |
| `POST` | `/api/v1/checks` | Create a check |
| `GET` | `/api/v1/checks/{slug}` | Get a check (secret fields are never returned) |
| `PUT` | `/api/v1/checks/{slug}` | Replace a check's settings |
| `DELETE` | `/api/v1/checks/{slug}` | Delete a check with its results |
| `POST` | `/api/v1/checks/validate` | Validate a config against a plugin without saving |
| `POST` | `/api/v1/checks/{slug}/probe` | Probe once now, without storing |
| `GET` | `/api/v1/checks/{slug}/results` | Latest raw results |
| `GET` | `/api/v1/checks/{slug}/rollups` | Hourly rollups (`from`, `to`) |
| `GET`, `POST` | `/api/v1/workers` | List or register workers (register returns a one-time enrollment token) |
| `GET`, `PUT`, `DELETE` | `/api/v1/workers/{slug}` | Get, replace or delete a worker |
| `POST` | `/api/v1/workers/{slug}/enrollment-token`, `/revoke` | New enrollment token; revoke the credential |
| `POST` | `/worker/v1/enroll` | Worker exchanges its enrollment token for a credential |
| `GET` | `/worker/v1/assignments` | Checks this worker must run (full snapshot with a revision) |
| `POST` | `/worker/v1/heartbeat` | Worker reports alive (`Authorization: Worker <credential>`) |
| `GET`, `POST` | `/api/v1/services` | List or create services |
| `GET`, `PUT`, `DELETE` | `/api/v1/services/{slug}` | Get (health, matched and uncovered checks), replace, delete |
| `GET` | `/api/v1/services/{slug}/health-changes` | Health transitions, newest first |
| `GET`, `POST` | `/api/v1/rules` | List or create rules |
| `GET`, `PUT`, `DELETE` | `/api/v1/rules/{slug}` | Get, replace or delete a rule |
| `GET` | `/api/v1/alerts` | Alerts newest first (`state`, `check`, `limit`) |
| `GET` | `/api/v1/alerts/{id}` | One alert |
| `POST` | `/api/v1/checks/{slug}/webhook-token` | Issue a new webhook token (shown once) |
| `GET`, `POST` | `/api/v1/hooks/{slug}/{webhook}` | Plugin webhooks, e.g. heartbeat `ping`. Token in `?token=` or `X-Vigia-Token` |

OpenAPI document at `/openapi/v1.json`.

## Configuration

| Section | Key | Default |
|---|---|---|
| `ConnectionStrings` | `Vigia` | local Postgres |
| `Database` | `MigrateOnStartup` | `true` |
| `Plugins` | `Path` | `plugins` |
| `Auth` | `OpenSignUp` | `false` |
| `Worker` | `Name`, `Region`, `BuiltInEnabled`, `MaxConcurrency`, `RefreshInterval`, `InitialJitter`, `ProbeTimeout` | `builtin`, `local`, `true`, `50`, `10s`, `10s`, `30s` |
| `Alerting` | `DefaultQuorum`, `HealthReconcileInterval` | `majority`, `1m` |
| `Retention` | `Enabled`, `RunInterval`, `RawResults`, `Rollups`, `RollupLookback` | `true`, `1h`, `14d`, `400d`, `48h` |

## Writing a check plugin

A plugin is an assembly plus a `plugin.json`, the single manifest (like Android's), described by [`schemas/plugin.schema.json`](schemas/plugin.schema.json). The manifest declares identity, the classes the host instantiates, dimensions and webhooks; the classes hold behavior only.

```json
{
  "$schema": "https://raw.githubusercontent.com/cl8dep/vigia/main/schemas/plugin.schema.json",
  "id": "acme.check.ping",
  "version": "1.0.0",
  "sdk": "1.x",
  "entry": "Acme.Check.Ping.dll",
  "label": "Ping",
  "description": "ICMP echo.",
  "check": {
    "class": "Acme.Check.Ping.PingCheck",
    "config": "Acme.Check.Ping.PingConfig",
    "dimensions": [{ "name": "latency", "direction": "higherIsWorse", "unit": "ms" }]
  }
}
```

```csharp
public sealed record PingConfig
{
    [Field("Host"), Required]
    public string Host { get; init; } = "";
}

public sealed class PingCheck : Check<PingConfig>
{
    public override async Task<ProbeResult> ProbeAsync(PingConfig config, ICheckContext ctx, CancellationToken ct)
    {
        // ...
        return ProbeResult.Up(Measurement.Latency(ms));
    }
}
```

Reference `Vigia.Plugins.Abstractions` with `Private=false` and `ExcludeAssets=runtime` and set `EnableDynamicLoading` (plugins in this repo get both from `plugins/Directory.Build.props`). See `plugins/Vigia.Check.Heartbeat` for a plugin with a webhook, and [docs/plugins.md](docs/plugins.md) for every manifest rule.

## Development

```bash
git config core.hooksPath .githooks   # pre-commit: dotnet format on staged C# + gitleaks (brew install gitleaks)
dotnet test                           # needs Docker for Testcontainers Postgres
tests/Vigia.IntegrationTests/bin/Debug/net10.0/Vigia.IntegrationTests -explicit only -diagnostics   # load tests (slow)
dotnet tool restore                   # dotnet-ef
dotnet ef migrations add <Name> --project src/Vigia.Infrastructure --startup-project src/Vigia.Api --output-dir Persistence/Migrations
```

Conventions are in [CLAUDE.md](CLAUDE.md): one type per file, XML docs, block-bodied methods, controllers over Minimal APIs.

## Layout

```
src/
  Vigia.Plugins.Abstractions   plugin SDK
  Vigia.Domain                 entities
  Vigia.Application            use cases (Mediator), worker runtime
  Vigia.Infrastructure         EF Core, Identity, plugin loading, background jobs
  Vigia.Api                    controllers, composition root
plugins/                       built-in plugins
tests/                         integration tests
docs/                          design documents
```

## License

Proprietary. All rights reserved. See [LICENSE](LICENSE).
