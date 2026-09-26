# Vigia

Self-hosted monitoring, on-call and AI-assisted incident investigation. Working name.

> **Status: early development.** The checks API, plugin system, built-in agent and result storage work. Alerting, notifications, on-call, remote agents and the AI layer are designed but not built yet. See [docs/](docs/).

## What works today

- **Plugin system.** Every check type is a separate assembly loaded at startup from `plugins/<id>/<version>/`, isolated in its own load context. The config schema is generated from the plugin's config record and attributes, and drives validation for the API (and later UI, YAML and Terraform).
- **HTTP check** (`vigia.check.http`) as the first built-in plugin.
- **Checks API.** Create, read, update, delete, validate config without saving, probe on demand.
- **Built-in agent.** Probes every enabled check on its interval with bounded concurrency, jitter and no overlapping probes. Emits metrics on meter `Vigia.Agent`.
- **Results.** Every probe is stored with exact timestamps. Hourly rollups keep per-dimension min / avg / max / p95. Raw results are kept 14 days, rollups 400 days.
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

OpenAPI document at `/openapi/v1.json`.

## Configuration

| Section | Key | Default |
|---|---|---|
| `ConnectionStrings` | `Vigia` | local Postgres |
| `Database` | `MigrateOnStartup` | `true` |
| `Plugins` | `Path` | `plugins` |
| `Auth` | `OpenSignUp` | `false` |
| `Agent` | `BuiltInEnabled`, `MaxConcurrency`, `RefreshInterval`, `InitialJitter`, `ProbeTimeout` | `true`, `50`, `10s`, `10s`, `30s` |
| `Retention` | `Enabled`, `RunInterval`, `RawResults`, `Rollups`, `RollupLookback` | `true`, `1h`, `14d`, `400d`, `48h` |

## Writing a check plugin

```csharp
public sealed record PingConfig
{
    [Field("Host"), Required]
    public string Host { get; init; } = "";
}

public sealed class PingCheck : Check<PingConfig>
{
    public override string Id { get { return "acme.check.ping"; } }

    public override CheckManifest Manifest { get; } = new()
    {
        Label = "Ping",
        Description = "ICMP echo.",
        ConfigType = typeof(PingConfig),
        Dimensions = [DimensionSpec.Latency],
    };

    public override async Task<ProbeResult> ProbeAsync(PingConfig config, ICheckContext ctx, CancellationToken ct)
    {
        // ...
    }
}
```

Reference `Vigia.Plugins.Abstractions` with `Private=false` and `ExcludeAssets=runtime`, set `EnableDynamicLoading`, and ship a `plugin.json` next to the assembly. See `plugins/Vigia.Check.Http` for a complete example.

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
  Vigia.Application            use cases (Mediator), agent runtime
  Vigia.Infrastructure         EF Core, Identity, plugin loading, background jobs
  Vigia.Api                    controllers, composition root
plugins/                       built-in plugins
tests/                         integration tests
docs/                          design documents
```

## License

Proprietary. All rights reserved. See [LICENSE](LICENSE).
