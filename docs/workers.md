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

- Configured per check (`quorum`: a count, a percentage, or `majority`). When a check does not set it, `Alerting:DefaultQuorum` applies (default `majority`: more than half; `50%` rounds up and is not a majority for even counts).
- Each worker keeps its own streak per rule: a worker "fails" a rule when its last `for` results match the condition.
- The workers that count are the **eligible workers that are online** (heartbeat within two minutes) plus any worker with a result in the last two intervals. An online worker that has not reported the check yet counts as not failing, so the first result after a deploy cannot fire alone. A worker that stopped reporting is lost visibility, not an outage.
- An alert fires when at least `quorum` of those workers fail the rule, and resolves when fewer than `quorum` have not recovered (each recovering after `recoverAfter` non-matching results).
- Webhook results (heartbeat pings) belong to the built-in worker's stream: both are the control plane.
- Alert messages name where it fails: "down from eu-west, us-east (2 of 3)".

## Build order

1. Rename agent to worker (done).
2. Tags replace labels, with `vigia:*` reconciliation (done: `vigia:plugin`; worker tags come with step 3).
3. Worker entity, enrollment tokens, worker credentials; built-in worker registered as a worker with `vigia:builtin` (done).
4. Placement (`workers.match`) and per-worker assignments; `unschedulable` / `workers-offline` states (done).
5. Quorum in rule evaluation (done).
6. Worker protocol (SignalR control + HTTPS data) and the `Vigia.Worker` binary reusing `WorkerScheduler`, with a local result buffer for degraded mode.
7. Plugin distribution from the control plane; fallback notifications (after notifiers exist).

## Enrollment (built)

1. An admin registers the worker: `POST /api/v1/workers { slug, name, region, tags }`. The response carries a one-time enrollment token (`vwe_...`), valid 24 hours; only its SHA-256 is stored.
2. The worker process exchanges it: `POST /worker/v1/enroll { token, version }` (no auth). It gets its credential (`vw_...`), again stored as a hash only. The token is consumed.
3. From then on the worker sends `Authorization: Worker <credential>` to `/worker/v1/*` (today: `heartbeat`). The worker scheme and the user scheme never overlap: a user token is rejected on `/worker/v1`, a worker credential on `/api/v1`.
4. Admins can revoke the credential (`POST /api/v1/workers/{slug}/revoke`), issue a new enrollment token to reinstall (`POST /api/v1/workers/{slug}/enrollment-token`; the old credential keeps working until the worker re-enrolls), or delete the worker.
5. A worker is `online` when it reported within two minutes.

The built-in worker registers itself on startup (slug `Worker:Name`, region `Worker:Region`), reports every minute, has no credential and cannot be deleted, enrolled or revoked.

## Placement (built)

- Checks take `workers: { match, quorum }`. `match` is a tag selector over worker tags, system tags included (`vigia:region`, `vigia:builtin`). `quorum` is stored and validated now (`2`, `"2"`, `"50%"`) and applied in step 5.
- Checks of plugins that declare webhooks (heartbeat) run only on the built-in worker, because receipts live on the control plane; a `workers.match` on them is rejected.
- Every check response carries `placement`: `state` (`ok`, `unschedulable`, `workers-offline`), `eligible` and `online` worker slugs.
- The built-in worker filters its own assignments by its tags. Remote workers read theirs from `GET /worker/v1/assignments`: a full snapshot with a `revision` that changes when any assigned check or the worker's own tags change. Assignments include check config with secrets, since the worker needs them to probe.

