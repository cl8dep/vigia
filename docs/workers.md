# Workers and multi-region

Decisions agreed on 2026-09-26. Protocol details are in [architecture.md](architecture.md).

## Vocabulary

- **Worker**: a process that runs checks. The control plane runs a built-in worker; remote workers run anywhere else (other regions, private networks, homelab). Never called "agent" in code; "agent" is reserved for the AI investigation agent.
- **Tag**: `key` or `key:value` on any entity, as in Piro (RFC 0008).

## Constraints

Workers often run inside internal networks **without internet access**. Therefore:

- A worker only needs an outbound connection to the control plane, which may be an internal URL. Supports `HTTPS_PROXY` and a private CA bundle for the control plane's TLS.
- Plugins reach workers from the control plane (or are baked into the worker image). Never from NuGet or the internet.
- Nothing is downloaded at runtime from third parties; no telemetry.
- Degraded-mode fallback notifications must be able to target internal channels (internal SMTP, internal webhook), not only ntfy.sh or Telegram. A failed fallback is logged, never retried forever.

## Tags

Taken from Piro, with the changes noted.

- Replace today's `labels` (`key=value` only) with tags on every entity: `key` (flag) or `key:value`.
- User keys: `^[a-z][a-z0-9_-]*$`, up to 63 chars; values up to 255; at most 50 tags per entity.
- `vigia:*` is reserved and **reconciled by the system** from facts on the entity; users cannot set it:

  | Tag | On | From |
  |---|---|---|
  | `vigia:plugin` | checks | plugin id (replaces the magic `plugin` key in rule selectors) |
  | `vigia:builtin` | workers | the in-process worker |
  | `vigia:region` | workers | the worker's configured region |

- Worker tags are assigned by an admin (API, YAML, Terraform), plus the reconciled `vigia:*` ones. A worker does not choose its own user tags.

## Placement

A check declares which workers may run it, separately from its own tags (a constraint on other entities, not a description of the check, same reasoning as Piro's `CheckRequiredWorkerTag`):

```yaml
workers:
  match: { region: [eu-west, us-east], network: vpc }   # AND across keys, OR within a key's values
  quorum: 2                                              # or "50%"
```

- No `match` means every worker.
- Unlike Piro's flat OR (a worker qualifies if it shares any required tag), keys combine with AND, so "internal network **and** region eu" is expressible. Piro's case ("eu or us") is one key with several values.
- A check no registered worker can match is `unknown` with reason `unschedulable` (config error). A check whose matching workers are all disconnected is `unknown` with reason `workers-offline` (transient). Neither is `down`.

## Quorum

- Configured per check (`quorum`: a number or a percentage). When a check does not set it, the global `Workers:DefaultQuorum` applies.
- Each worker keeps its own streak per rule: a worker "fails" a rule when its last `for` results match the condition.
- An alert fires when at least `quorum` of the workers with fresh results fail the rule, and resolves when fewer than `quorum` still fail (each worker recovering after `recoverAfter` non-matching results).
- A worker without a result in the last two intervals does not count either way: lost visibility from that region is not an outage.
- Alert messages name where it fails: "down from eu-west, us-east (2 of 3)".

## Build order

1. Rename agent to worker (done).
2. Tags replace labels, with `vigia:*` reconciliation (done: `vigia:plugin`; worker tags come with step 3).
3. Worker entity, enrollment tokens, worker credentials; built-in worker registered as a worker with `vigia:builtin` (done).
4. Placement (`workers.match`) and per-worker assignments; `unschedulable` / `workers-offline` states.
5. Quorum in rule evaluation.
6. Worker protocol (SignalR control + HTTPS data) and the `Vigia.Worker` binary reusing `WorkerScheduler`, with a local result buffer for degraded mode.
7. Plugin distribution from the control plane; fallback notifications (after notifiers exist).

## Enrollment (built)

1. An admin registers the worker: `POST /api/v1/workers { slug, name, region, tags }`. The response carries a one-time enrollment token (`vwe_...`), valid 24 hours; only its SHA-256 is stored.
2. The worker process exchanges it: `POST /worker/v1/enroll { token, version }` (no auth). It gets its credential (`vw_...`), again stored as a hash only. The token is consumed.
3. From then on the worker sends `Authorization: Worker <credential>` to `/worker/v1/*` (today: `heartbeat`). The worker scheme and the user scheme never overlap: a user token is rejected on `/worker/v1`, a worker credential on `/api/v1`.
4. Admins can revoke the credential (`POST /api/v1/workers/{slug}/revoke`), issue a new enrollment token to reinstall (`POST /api/v1/workers/{slug}/enrollment-token`; the old credential keeps working until the worker re-enrolls), or delete the worker.
5. A worker is `online` when it reported within two minutes.

The built-in worker registers itself on startup (slug `Worker:Name`, region `Worker:Region`), reports every minute, has no credential and cannot be deleted, enrolled or revoked.

