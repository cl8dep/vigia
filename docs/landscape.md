# Landscape

Snapshot as of 2026-09-26.

## Monitoring, status pages, on-call

| Project | Stars | Stack / license | Strengths | Weaknesses |
|---|---|---|---|---|
| [heva-co/piro](https://github.com/heva-co/piro) | 8 | .NET 10, AGPL | Multi-region workers, on-call, escalation, status page, config as code, plugin model | Very young, single vendor, no AI, workers die with the API |
| [louislam/uptime-kuma](https://github.com/louislam/uptime-kuma) | 92k | Node, MIT | Huge community, simple UX, many notifiers | Single instance, no multi-region, no on-call |
| [TwiN/gatus](https://github.com/TwiN/gatus) | 12k | Go, Apache | YAML config, lightweight, expressive conditions | No management UI, no incidents, no on-call |
| [openstatusHQ/openstatus](https://github.com/openstatusHQ/openstatus) | 9k | TS, AGPL | Multi-region, modern status page | SaaS first, self-host is secondary |
| [OneUptime/oneuptime](https://github.com/OneUptime/oneuptime) | 7.6k | TS, custom | Does everything | Heavy, complex, unclear license |
| [bluewave-labs/Checkmate](https://github.com/bluewave-labs/Checkmate) | 11k | TS, AGPL | Infra + uptime, good UI | No on-call |
| [healthchecks/healthchecks](https://github.com/healthchecks/healthchecks) | 10k | Python, BSD | Best at cron / heartbeat | Only that |
| Grafana OnCall, Netflix Dispatch | - | - | Were the on-call / incident reference | Both archived |

## Alert intelligence and AI investigation

| Project | Stack / license | Strengths | Weaknesses |
|---|---|---|---|
| [resolve.ai](https://resolve.ai) | SaaS | Agent teams investigate code, infra, telemetry in parallel; guardrailed remediation; context graph; MCP | SaaS only, enterprise pricing |
| [wachd/wachd](https://github.com/wachd/wachd) | Go, Apache (open core) | Inbound webhooks, outbound collectors, PII sanitizer, Ollama, graceful degradation without AI, solid Helm | Hardcoded OSS limits (1 team, 5 users, 1000 alerts/month), single-prompt RCA, weekly-only on-call, single maintainer |
| [HolmesGPT](https://github.com/HolmesGPT/holmesgpt) | Python, Apache, CNCF | Real tool-calling investigation agent | Investigation only |
| [keephq/keep](https://github.com/keephq/keep) | Python | Alert dedup, correlation, workflows | No monitoring, no on-call |

## What to take

- **Piro:** plugin model with attributes and generated schema, on-call and escalation, config as code with `plan` / `apply`, worker tags.
- **Wachd:** inbound webhook + outbound collectors, alert delivered even if AI fails, swappable LLM backend, PII sanitizing before the LLM.
- **HolmesGPT / resolve.ai:** iterative, tool-calling investigation instead of a single prompt; MCP as the integration surface.
- **Uptime Kuma:** install simplicity and breadth of notifiers.

## The gap

Detection + response + AI investigation, self-hosted, no artificial limits, light enough for a homelab.
