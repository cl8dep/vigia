# Vigia docs

Working name. Design docs for a self-hosted monitoring, on-call and AI-assisted incident investigation platform.

| Doc | What it covers |
|---|---|
| [vision.md](vision.md) | Problem, audiences, principles, non-goals |
| [landscape.md](landscape.md) | Existing tools, what to take from each, the gap |
| [features.md](features.md) | Feature set by phase |
| [architecture.md](architecture.md) | Control plane, workers, degraded mode, data flow |
| [scheduling-and-alerting.md](scheduling-and-alerting.md) | Piro review, worker runtime, results storage, rule evaluation |
| [services.md](services.md) | Services, dependencies, health from alerts, curated status pages |
| [workers.md](workers.md) | Workers, tags, placement, quorum, multi-region decisions |
| [domain.md](domain.md) | Entities, what changes vs Piro, status derivation |
| [config-as-code.md](config-as-code.md) | YAML + CLI, Terraform provider, Flystern example |
| [plugins.md](plugins.md) | Plugin model: checks, sources, notifiers, context providers |
| [open-questions.md](open-questions.md) | Decisions still pending |

Status: brainstorming. Nothing here is final.
