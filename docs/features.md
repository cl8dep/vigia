# Features

Phases are a draft. Every item below is a plugin or uses the plugin model unless noted.

## MVP: core, no AI

### Monitoring
- Check plugins: HTTP, TCP, DNS, TLS certificate, ping, heartbeat (inbound).
- DNS done properly: query multiple resolvers (authoritative, public, internal) and compare, expected-record drift detection, domain expiry.
- Distributed agents with autonomous degraded mode (see [architecture.md](architecture.md)).
- Agent selection by labels (`region=eu`, `network=flystern-vpc`).
- Quorum: a check is down only when N of M agents agree.

### Alerts
- Inbound source plugins: Prometheus Alertmanager, Grafana, generic webhook.
- Dedup and grouping by fingerprint.
- Alert rules with failure / recovery thresholds to cut flapping.

### Response
- On-call schedules: rotations, overrides, timezones.
- Escalation policies: ordered steps, delays, repeat.
- Acknowledge / resolve from the notification itself.
- Notifier plugins: email, Telegram, ntfy, Slack, generic webhook.

### Status page
- Public status page exportable as a static site to any bucket or CDN.

### Platform
- Single binary, SQLite or Postgres.
- YAML config with `plan` / `apply`.
- REST API.
- Labels on every entity (services, checks, agents, alerts).

## v1: AI layer

- On alert or incident, an agent gathers context through context provider plugins (recent deploys, commits, logs, metrics, related checks) and posts a summary citing evidence.
- Outside-in vs inside-out comparison fed to the agent ("fails from public regions, passes from inside the VPC").
- AI backend plugins: Ollama, OpenAI-compatible, Anthropic.
- PII redaction before any data reaches the LLM.
- Read-only tools exposed over MCP.
- Incidents with timeline.

## Later

- SSO (OIDC, SAML), RBAC, audit log.
- Postmortems.
- AI-suggested actions with human approval (silence, rollback, open PR).
- Helm chart with HA.
- More sources (Datadog, Sentry, CloudWatch) and notifiers (SMS, Teams, Google Chat).
- Third-party plugins loaded at runtime.

## Never

See non-goals in [vision.md](vision.md).
