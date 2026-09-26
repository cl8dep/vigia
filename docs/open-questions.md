# Open questions

| # | Question | Options | Leaning |
|---|---|---|---|
| 1 | Product name | - | `vigia` is provisional |
| 2 | License | Proprietary for now (all rights reserved); open-source choice (Apache 2.0, AGPL, MIT) deferred | Proprietary |
| 3 | Start from scratch or reuse code | Scratch, fork Piro (AGPL), borrow ideas from Wachd (Apache) | Scratch, borrow ideas |
| 4 | Where the control plane runs for Flystern | Same AWS infra, isolated cloud account / VPS, homelab | Isolated VPS |
| 5 | ~~Worker to control plane transport~~ | Resolved: SignalR for control, HTTPS for data | - |
| 6 | ~~Duplicate fallback alerts in degraded mode~~ | Resolved: no coordination, each worker notifies on transitions; reconciled on reconnect | - |
| 7 | ~~Schema generation vs AOT for workers~~ | Resolved: runtime plugin loading rules out AOT; reflection at load time | - |
| 8 | Frontend stack | React SPA, Blazor, server-rendered | - |
| 9 | First Flystern-specific check | Sabre availability, booking flow synthetic, DNS | - |
| 10 | Is our own on-call worth building vs integrating PagerDuty-style tools | Build, integrate, both | Build (core value for small teams) |
| 11 | Time-series storage for results | Same DB with retention, TimescaleDB, separate store | Same DB with rollups |
| 12 | Opt-in automatic incident creation | Never, opt-in per service | - |
| 13 | Team membership model | Global roles only, roles per team | Roles per team |
| 14 | Plugin signing | Hash only, signing key per publisher, org-level trust list | Hash in phase 1, signing later |
| 15 | Plugin package format | Plain zip + `plugin.json`, `.nupkg` | Plain zip |
| 16 | Dependency-aware inhibition: suppress or only annotate consequence alerts | Suppress escalation, annotate only, configurable per dependency mode | Suppress for `blocking`, annotate for `soft` / `advisory` |
| 17 | YAML layout | Single file, directory of files merged, k8s-style `kind` documents | Directory of files merged |
| 18 | Scheduler for control plane jobs (retention, rollups, maintenance) | `BackgroundService` now, Quartz with clustering from the start | `BackgroundService` now, Quartz when multi-replica |
