# Services and status pages

Decisions agreed on 2026-09-26.

## What the industry does

| Product | What a "service" is | Health | Take | Avoid |
|---|---|---|---|---|
| Statuspage (Atlassian) | Public "component", in groups | Manual or via integrations | Curated public view, separate from internals; `partial outage` state | Not tied to monitoring; someone must update it |
| PagerDuty | Unit of ownership and routing: integration -> service -> escalation policy; technical vs business services | Open incidents | Service as the routing unit for on-call | Weight; no monitoring of its own |
| Piro | Owns its checks (1:N), dependencies blocking / soft / advisory, escalation policy per service; per-rule `MinFailingRegions` | Derived from checks + dependency cascade | Dependencies, propagation modes, rule-level quorum, system tag catalog | A check belonging to one service; derived status stored without reconciliation |
| Better Stack, Uptime Kuma, OpenStatus | Not an entity: monitors grouped on a status page | Monitor state | Zero modeling for small installs | No ownership, dependencies or routing |
| Gatus | A `group` field on each endpoint | Endpoint state | Minimal | Display only |
| Backstage, incident.io catalog, Datadog | Catalog entry (component, system, domain) with owner and `dependsOn` | Not computed (Datadog via SLOs) | Ownership and graph feed investigation | It is a catalog, not a monitor |

## Tags: system tag catalog

Extends [workers.md](workers.md). Follows Piro's RFC 0008 catalog.

- **One value per key**, as Kubernetes, Prometheus, AWS and Piro. Many-to-many grouping is expressed on the selecting side (a service's selector), never with multi-valued tags. Keys used for routing and ownership (`team`, `env`, `region`) must be single-valued anyway.
- Every `vigia:*` key has a catalog definition:

  | Property | Values |
  |---|---|
  | Assignment | `reconciled` (derived from a fact on the entity; users cannot set it), `assignable` (users toggle it; the key and its meaning are Vigia's), `computed` (evaluated on read, never stored) |
  | Value | `flag` (key only), `value` (free string), `vocabulary` (closed list) |
  | Entities | Where the key applies |

- Assignable system tags are written in the same `tags` object as user tags and validated against the catalog; reconciled and computed ones are rejected there. API, YAML and Terraform look the same:

  ```yaml
  tags: { team: payments, "vigia:external": null }
  ```

- Catalog so far:

  | Key | Assignment | Value | Entities |
  |---|---|---|---|
  | `vigia:plugin` | reconciled | value | checks |
  | `vigia:builtin` | reconciled | flag | workers |
  | `vigia:region` | reconciled | value | workers (where the worker runs) |
  | `vigia:external` | assignable | flag | services, checks (a provider: Sabre, Stripe, a DNS provider) |

- Naming convention: `region` (user tag on checks) is **where the target is deployed**; `vigia:region` (system tag on workers) is **where the probe runs from**.

## Service

- `slug`, `name`, `tags`, `managedBy` like every entity. External providers carry `vigia:external` (replaces a `kind` field).
- `checks`: a tag selector over checks (`{ match: { service: booking-api } }`). A check can feed several services. One direction only: services select checks; checks never point to services.
- `partitionBy`: optional check tag key (usually `region`) for services deployed in several places. See Health.
- Later, when those entities exist: owning `team` and `escalationPolicy` (the PagerDuty routing unit).

## Dependencies

- `service -> dependsOn` with a mode, as in Piro:

  | Mode | Effect of a failing dependency |
  |---|---|
  | `blocking` | The dependent is down too; its consequence alerts are linked to the cause and do not escalate on their own |
  | `soft` | The dependent is degraded |
  | `advisory` | Shown for context only |

- Cycles are rejected when the edge is created.

## Health

States, worst first: `down`, `partial-outage`, `degraded`, `maintenance`, `operational`, `unknown` (no checks matched, or every check unschedulable).

1. **Per check**, from its firing alerts: any `critical` -> down; any `warning` -> degraded; else operational. `info` alerts do not affect health. This inherits quorum, thresholds and anti-flapping from rules.
2. **Per service without `partitionBy`**: worst of its checks.
3. **Per service with `partitionBy: <key>`**: checks are grouped by that tag; each group takes its worst state; then all groups down -> `down`, some groups down -> `partial-outage`, else the worst remaining (`degraded` or `operational`). Checks without the key form their own group. Works for any key: `region`, `az`, `cluster`, `shard`. The response shows the per-partition breakdown ("partial outage: eu-west down").
4. **Dependencies** propagate, following the mode table.
5. **Overrides** on top: maintenance window, then a manual override with a message.

Checks without any rule never affect a service; the service response lists them as `uncoveredChecks` so this is visible.

### Stored or computed: hybrid

- One function defines health (steps above).
- It runs when an input changes (an alert opens, updates or resolves; a dependency, override or maintenance changes), in the same transaction, for the affected services and their dependents. It stores the current state and appends a transition to `service_health_changes` when the state changes.
- A periodic job recomputes every service, to apply time-based inputs (maintenance windows starting and ending) and to repair any drift.
- A test asserts that the stored state always equals a fresh recomputation.
- Reads (API, public status page) use the stored state; transitions feed notifications and status page history.

## Multi-region services

Two shapes, both supported:

1. **Global endpoint** (anycast, CDN, geo-DNS): one check observed from several worker regions (`workers.match: { "vigia:region": [eu-west, us-east] }`). Alert messages say where it fails. To express "any region failing is degraded, most regions failing is down", **rules may override the check's quorum**:

   ```yaml
   rules:
     booking-regional: { selector: { service: booking-api }, when: { outcome: down }, quorum: 1, severity: warning }
     booking-global:   { selector: { service: booking-api }, when: { outcome: down }, quorum: majority, severity: critical }
   ```

2. **Regional deployments** (active-active, one endpoint per region): one check per deployment region, tagged `region: <where it runs>`, placed on nearby or in-network workers; the service sets `partitionBy: region`, so one region down is `partial-outage`, not `down`.

   ```yaml
   checks:
     booking-eu: { tags: { service: booking-api, region: eu-west }, workers: { match: { network: eu-vpc } } }
     booking-us: { tags: { service: booking-api, region: us-east }, workers: { match: { network: us-vpc } } }
   services:
     booking-api: { checks: { match: { service: booking-api } }, partitionBy: region }
   ```

## Status page (separate, curated)

- Nothing is public by default. A status page lists **components**, each pointing to one service, with a public name and an optional group:

  ```yaml
  status-pages:
    public:
      title: Flystern status
      components:
        - { service: booking-api, name: "Bookings", group: "Core" }
        - { service: sabre, name: "Flight search provider", group: "Providers" }
  ```

- Shows component health (with partition breakdown), hourly availability from rollups, and later incidents.
- Served read-only without authentication at `GET /status/{page}` (JSON) for the demo and embeds; the static export to a bucket or CDN (see [architecture.md](architecture.md)) comes after.

## API

| Method | Route | Purpose |
|---|---|---|
| `GET`, `POST` | `/api/v1/services` | List or create |
| `GET`, `PUT`, `DELETE` | `/api/v1/services/{slug}` | Get (with health and breakdown), replace, delete |
| `GET` | `/api/v1/services/{slug}/health-changes` | Transition history |
| `GET`, `POST` | `/api/v1/status-pages` | List or create |
| `GET`, `PUT`, `DELETE` | `/api/v1/status-pages/{slug}` | Get, replace, delete |
| `GET` | `/status/{slug}` | Public, unauthenticated, read-only status of a page |

## Build order

1. System tag catalog (`reconciled` / `assignable` / `computed`, `flag` / `value` / `vocabulary`) with `vigia:external`; rule-level `quorum`.
2. Service entity, CRUD, check selector, `partitionBy`, dependencies with cycle check.
3. Health: the function, stored state, transition history, recompute on alert changes, reconciliation job.
4. Status page entity and the public read-only endpoint.
5. Demo: docker compose, seed data, `index.html` rendering the public status page.
6. Later: maintenance windows, manual overrides, teams and escalation policies on services, consequence-alert inhibition for `blocking` dependencies, static export.
