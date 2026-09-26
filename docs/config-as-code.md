# Config as code

Two supported front ends over the same declarative API:

- **`vigia.yaml` + `vigia` CLI** (`plan` / `apply` / `export`). Zero extra tooling, good for homelab and small teams.
- **Terraform provider** (`vigia`). For teams that already manage infra with Terraform / Terragrunt.

Both are clients of the REST API. Neither has special powers the API lacks.

## Principles

- **Slug is identity.** Every entity is addressed by `slug`, stable across instances. Ids are internal.
- **Ownership.** `managedBy: ui | yaml | terraform`. An entity owned by one front end is read-only for the others (the UI shows who owns it). Changing owner is an explicit action (`vigia adopt`, `terraform import`).
- **Partial management.** What the file does not declare, it does not touch (same as Piro).
- **No secrets in files.** Secret fields take references (`${env:NAME}`, `${file:path}`), resolved by the CLI at apply time. Terraform uses its own variables / secret stores.
- **Users are referenced, not created.** Users come from invitations or SSO. Config refers to them by email.
- **Plugin config is validated by the plugin schema.** Same schema the UI uses; the CLI and Terraform ask the instance to validate at plan time.

## Example: Flystern

```yaml
version: 1

teams:
  platform:
    name: Platform
    members:
      - { user: alice@flystern.example, role: admin }
      - { user: bob@flystern.example,   role: member }
      - { user: carol@flystern.example, role: member }

schedules:
  platform-primary:
    team: platform
    timezone: America/New_York
    layers:
      - name: weekly
        rotation: { every: 1w, handoff: "mon 09:00" }
        users: [alice@flystern.example, bob@flystern.example, carol@flystern.example]

escalation-policies:
  platform-default:
    team: platform
    steps:
      - targets: [{ schedule: platform-primary }]
        repeat: 2
        repeat-after: 5m
      - after: 15m
        targets: [{ team: platform }]

  # Vendor outages: we cannot fix them at 3am, only communicate.
  vendor-notify:
    team: platform
    steps:
      - targets: [{ channel: slack-ops }]

integrations:
  slack-ops:
    plugin: vigia.notifier.slack@1
    config:
      webhook-url: ${env:SLACK_OPS_WEBHOOK}
  ntfy-fallback:
    plugin: vigia.notifier.ntfy@1
    config:
      server: https://ntfy.sh
      topic: flystern-ops-fallback
      token: ${env:NTFY_TOKEN}
  alertmanager:
    plugin: vigia.source.alertmanager@1
    labels: { env: prod }

agents:
  # Agents enroll themselves; config only sets policy per label group.
  prod:
    selector: { env: prod }
    fallback:
      channels: [ntfy-fallback]
      notify-after: 2m

services:
  booking-api:
    team: platform
    labels: { env: prod, tier: critical }
    checks: { selector: { service: booking-api } }
    depends-on:
      - { service: sabre, mode: blocking }
      - { service: dns-flystern, mode: blocking }
    escalation-policy: platform-default

  sabre:
    kind: external
    team: platform
    labels: { vendor: sabre }
    checks: { selector: { service: sabre } }
    escalation-policy: vendor-notify

  dns-flystern:
    kind: external
    team: platform
    checks: { selector: { service: dns-flystern } }
    escalation-policy: platform-default

checks:
  booking-api-health:
    plugin: vigia.check.http@1
    interval: 30s
    labels: { service: booking-api }
    agents: { selector: { env: prod }, quorum: 2 }
    config:
      url: https://api.flystern.example/health
      expected-status: [200]
      timeout: 5s

  booking-api-internal:
    plugin: vigia.check.http@1
    interval: 30s
    labels: { service: booking-api, view: inside }
    agents: { selector: { network: flystern-vpc } }
    config:
      url: http://booking-api.internal:8080/health

  sabre-availability:
    plugin: vigia.check.http@1
    interval: 1m
    labels: { service: sabre }
    agents: { selector: { network: flystern-vpc } }
    config:
      url: https://api.sabre.example/v1/ping
      headers:
        Authorization: Bearer ${env:SABRE_TOKEN}

  flystern-dns:
    plugin: vigia.check.dns@1
    interval: 1m
    labels: { service: dns-flystern }
    agents: { selector: { role: public-probe }, quorum: 50% }
    config:
      host: api.flystern.example
      record-type: A
      resolvers: [authoritative, 1.1.1.1, 8.8.8.8]
      expected: [203.0.113.10]

  flystern-tls:
    plugin: vigia.check.tls@1
    interval: 1h
    labels: { service: booking-api }
    agents: { selector: { role: public-probe }, quorum: 1 }
    config:
      host: api.flystern.example

rules:
  # Selector rules apply to every matching check.
  http-down:
    selector: { plugin: vigia.check.http }
    when: { dimension: status, is: down }
    for: 3
    recover-after: 2
    severity: critical

  slow-api:
    selector: { service: booking-api, plugin: vigia.check.http }
    when: { dimension: latency, above: 800ms }
    for: 5
    severity: warning

  tls-expiry:
    selector: { plugin: vigia.check.tls }
    when: { dimension: days-to-expiry, below: 14 }
    severity: warning

status-pages:
  public:
    title: Flystern status
    services: [booking-api, sabre]
    publish: { plugin: vigia.publisher.s3@1, config: { bucket: status-flystern } }
```

## Same thing in Terraform

```hcl
resource "vigia_team" "platform" {
  slug = "platform"
  name = "Platform"
}

resource "vigia_check" "booking_api_health" {
  slug     = "booking-api-health"
  plugin   = "vigia.check.http@1"
  interval = "30s"
  labels   = { service = "booking-api" }

  agents = {
    selector = { env = "prod" }
    quorum   = 2
  }

  config = {
    url             = "https://api.flystern.example/health"
    expected_status = [200]
    timeout         = "5s"
  }
}

resource "vigia_service" "booking_api" {
  slug              = "booking-api"
  team              = vigia_team.platform.slug
  checks_selector   = { service = "booking-api" }
  escalation_policy = vigia_escalation_policy.platform_default.slug

  depends_on_services = [
    { service = vigia_service.sabre.slug, mode = "blocking" },
  ]
}
```

### Provider design

- Written in Go with the Terraform Plugin Framework (the only Go component).
- One resource per entity kind, not per plugin: `vigia_check`, `vigia_integration`, `vigia_rule`, ...
- Plugin config is a dynamic attribute. The provider cannot know plugin schemas at compile time since plugins are loaded at runtime. At plan time it calls `POST /api/v1/checks/validate` on the instance, which validates against the installed plugin schema and returns field-level errors.
- Import by slug: `terraform import vigia_check.x booking-api-health`.
- Data sources for things Terraform should not own: `vigia_user`, `vigia_agent`, `vigia_plugin`.

### Bootstrapping from an existing instance

`terraform init` only downloads the provider; it never talks to the instance. Existing config is pulled in two ways:

- **Terraform import with config generation** (Terraform 1.5+): `import` blocks by slug, then `terraform plan -generate-config-out=generated.tf`. Requires solid `Read` and `Import` in every resource.
- **`vigia export`** writes everything in one go: `--format yaml` or `--format terraform` (resources + `import` blocks).

### Plugin schemas in Terraform

Terraform asks the provider for its schema before the provider is configured, so the provider cannot fetch schemas from the instance at that point.

- **Base:** `config` is a dynamic attribute validated against the instance at plan time (same pattern as `kubernetes_manifest` with CRDs). Field-level errors at `plan`, no per-plugin autocomplete.
- **Optional:** `vigia schema --format terraform` generates a typed wrapper module per installed plugin (`object({...})` variables around `vigia_check`), for autocomplete and early type errors.
- **Rejected:** dynamic provider schema from `VIGIA_URL` at startup. Discouraged by HashiCorp; the schema would change with installed plugins and can break state.

## What the example surfaced

Writing a real config exposed decisions the domain model did not cover yet:

1. **Rules from several places.** A check can match several selector rules (`http-down` and `slow-api`). Decision: rules are additive; each produces its own alerts. No precedence, no merging.
2. **Quorum as number or percentage.** With a selector, the number of matching agents changes over time. `quorum` accepts `2` or `50%`; default is majority.
3. **External services route differently.** Paging someone at 3am for a Sabre outage is useless. External services get their own policy (`vendor-notify`), and the status page shows "degraded due to a provider".
4. **Dependency-aware inhibition.** When `sabre` is down and `booking-api` fails because of it, the booking alert should be linked as a consequence and not escalate on its own. Similar to Alertmanager inhibition, but derived from the dependency graph instead of hand-written rules.
5. **Service to checks direction.** Services select checks by label; checks carry `service: x`. One direction only, to avoid two sources of truth.
6. **Status page publisher is a plugin.** New plugin kind: `Publisher` (S3, Cloudflare Pages, local folder).
7. **Inside vs outside views.** Labeling checks with `view: inside` lets the investigation compare them with public ones automatically.
