# Vigia

Self-hosted monitoring, on-call and AI-assisted incident investigation. Design docs in `docs/`.

## Stack

- .NET 10, C# latest, nullable enabled, warnings as errors.
- Clean Architecture: `Domain` <- `Application` <- `Infrastructure` <- `Api`.
- Mediator (martinothamar/Mediator, MIT, source generated). Not MediatR (commercial license).
- EF Core + Npgsql (Postgres). SQLite later, with its own migrations project.
- ASP.NET Core Identity.
- Central package management in `Directory.Packages.props`.

## Conventions

- **One type per file.** No multiple classes, records, interfaces or enums in the same `.cs`. File name matches the type name.
- **XML doc comments on almost everything.** Every public or protected type and member gets a `<summary>`. Skip only private members and the truly obvious (for example a property named `Name` on an entity).
- **Methods always use block bodies `{ }`**, never `=>`. Same for constructors, operators and local functions. Enforced by `.editorconfig` at build time. Expression-bodied read-only properties are fine.
- **HTTP API uses controllers** (`[ApiController]`, one controller per resource in `Vigia.Api/Controllers`), not Minimal APIs. Controllers only translate HTTP to Mediator requests.
- Comments inside method bodies only explain a non-obvious why.
- Plugins never ship `Vigia.Plugins.Abstractions`; it is always resolved from the host.
- `plugin.json` is a plugin's only manifest (like `AndroidManifest.xml`): identity, component classes, dimensions, webhooks. Plugin classes never carry ids, labels or manifests in code. Any manifest change updates `PluginManifest` and `schemas/plugin.schema.json` together (`ManifestSchemaTests` fails otherwise).

## Layout

```
src/
  Vigia.Plugins.Abstractions   plugin SDK (published as NuGet)
  Vigia.Domain                 entities, no dependencies
  Vigia.Application            use cases (Mediator handlers), abstractions
  Vigia.Infrastructure         EF Core, Identity, plugin loading
  Vigia.Api                    HTTP endpoints, composition root
plugins/                       built-in plugins, each loaded at runtime from artifacts/plugins
tests/                         integration tests (Testcontainers Postgres)
```
