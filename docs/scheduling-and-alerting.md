# Scheduling, results and alerting

Review of how Piro does it (commit `7761090`) and what Vigia does instead.

## How Piro does it

```
Quartz cron trigger (per check, Postgres job store)
  -> CheckExecutionJob -> CheckRunnerService -> RoutingCheckJobDispatcher
       -> picks eligible workers by tags
       -> RemoteCheckJobDispatcher: push "Execute" over SignalR to each worker
          (the built-in API worker runs in-process through the same path)
  -> each worker result -> CheckDataPoint (timestamp truncated to the minute)
  -> MultiRegionBatchTracker (in memory): waits for N results or 60s
       -> aggregates: all down = DOWN, some down = PARTIALLY_DOWN
  -> CheckResultIngester: updates Check.CurrentStatus, pushes to an in-memory Channel
       -> StatusDrainHostedService recomputes service status with dependency cascade
  -> AlertEvaluationService
       -> loads recent data points, groups by minute "cycle"
       -> a cycle fails when >= MinFailingRegions points meet the condition
       -> fires after FailureThreshold failing cycles, recovers after SuccessThreshold
       -> state kept in AlertConfig.IsAlerting
  -> AlertLifecycleService: dedup by fingerprint = normalized message
  -> NotificationEventOutbox (DB) -> NotificationDispatchWorker (lease, retries, ordering)
```

Relevant files: `Infrastructure/Jobs/CheckSchedulerService.cs`, `Infrastructure/Workers/RemoteCheckJobDispatcher.cs`, `Application/Services/MultiRegionBatchTracker.cs`, `Application/Services/AlertEvaluationService.cs`, `Application/Services/AlertLifecycleService.cs`, `Infrastructure/Notifications/NotificationDispatchWorker.cs`.

## Keep

- **Built-in worker as a normal worker.** The API registers itself as a worker and runs checks through the same path as remote ones. Single-node installs need nothing else.
- **"No data" is not "down".** When no worker can run a check Piro writes `MONITOR_OUTAGE` or `UNSCHEDULABLE` data points instead of marking the target down.
- **Failure / success thresholds and minimum failing regions per rule.** The evaluation model (consecutive failing cycles, quorum per cycle) is right.
- **Notification outbox.** DB table, lease for stuck rows, bounded retries, per-key ordering so "resolved" never goes out before "fired".
- **Evaluation reads stored results**, so it survives restarts and is easy to reason about.

## Change

| # | Piro | Problem | Vigia |
|---|---|---|---|
| 1 | The control plane fires every probe (Quartz) and pushes it to workers | Workers cannot run without the API; contradicts agent degraded mode | Agents schedule locally from their cached assignments. The control plane only assigns. The built-in agent uses the same runtime |
| 2 | Quartz with a Postgres job store, one job + cron trigger per check | Remote agents have no database, so Quartz there loses persistence and clustering (its main strengths); misfire catch-up is useless for probes | Own scheduler inside the agent runtime: one loop over a priority queue ordered by next run, bounded concurrency, jitter. See "Why not Quartz for probes" |
| 3 | Data point timestamps truncated to the minute | Checks faster than 1/min overwrite each other; cycles are artificial | Exact timestamps. Quorum uses the latest result per agent within a freshness window |
| 4 | Multi-region batch aggregated in memory with a fixed 60s timeout | Lost on restart; slow agents always wait for the timeout | Quorum computed from stored results on each ingest; no in-memory batches |
| 5 | Check status change goes through an in-memory `Channel` | Lost on crash; service status can go stale | Status recomputation is triggered through the outbox, like notifications |
| 6 | `IsAlerting` stored on `AlertConfig` | Mixes rule config and runtime state; cannot work for a rule that targets many checks by selector | State lives on the alert, keyed by `(rule, check)` |
| 7 | Alert fingerprint = normalized message | A new error text ("timeout" -> "connection refused") resolves the alert and opens a new one: re-paging and noise | Fingerprint = rule + check (+ source labels for inbound). The message is an attribute that updates |
| 8 | Alert messages built with a `switch` over dimension names (`"Latency"`, `"CertExpiry"`) | Core knows plugin dimensions; contradicts the plugin model | Messages built from `DimensionSpec` (name, unit, direction) plus `ProbeResult.Message` |
| 9 | No retention for data points | The table grows forever | Raw results kept N days (default 14), hourly rollups kept longer, pruned by a background job |

## Vigia design

### Agent runtime (shared by built-in and remote agents)

```
AssignmentSource ──> AgentScheduler ──> ProbeExecutor ──> ResultSink
 (in-process or       (timer per check,   (plugin + timeout,   (in-process: DB
  SignalR/HTTPS)       jitter, no overlap)  error isolation)     remote: HTTPS batch + local buffer)
```

- `AssignmentSource` for the built-in agent reads enabled checks from the DB and reacts to changes; for remote agents it is the protocol in [architecture.md](architecture.md).
- `AgentScheduler` is a single loop over a `PriorityQueue` keyed by next run time. It dispatches to a bounded pool (for example 50 concurrent probes per agent), adds jitter, never runs two probes of the same check at once, and a slow probe skips the next tick instead of piling up.
- `ProbeExecutor` is what `ProbeCheckHandler` does today, moved to one place.

### Storage

| Table | Content |
|---|---|
| `results` | One row per probe: check, agent, outcome, measurements (`jsonb`), message, duration, `observed_at` (exact). Indexed by `(check_id, observed_at desc)` |
| `result_rollups` | Per check, agent and hour: counts by outcome, latency avg / p95 / max |
| `check_states` | Derived cache per check: current outcome, since when, latest result per agent. Recomputed on ingest |

### Evaluation on ingest

1. Store the result.
2. Recompute the check state: latest result per assigned agent within `2 x interval`; apply quorum (`N` or `%`).
3. For each rule that targets the check (by id or selector): count consecutive evaluations meeting the condition; fire after `for`, recover after `recover-after`.
4. Alert changes and check state changes are written to the outbox in the same transaction.
5. Outbox workers deliver notifications and recompute service status.

### No-data reasons

A check with no fresh result from enough agents is `unknown`, with a reason: `no-agent` (nothing matches the selector), `agents-offline` (matching agents disconnected), `stale` (agents connected but not reporting). Never `down`.

## Why not Quartz for probes

Quartz itself supports second-level schedules and clustering; Piro's minute granularity and single replica are Piro's choices (standard 5-field cron, timestamp truncation, clustering disabled on purpose). The reason not to use it for probes is where scheduling runs:

- Probes are scheduled by agents, and remote agents have no database. Quartz would run on its RAM store there, without persistence or clustering.
- Missed probe ticks must be skipped, not caught up. Quartz misfire handling is built for the opposite.
- Thousands of checks that change often would mean one job + trigger per check and constant rescheduling. A priority queue handles that in memory with no dependency.

## Control plane jobs

Retention pruning, rollups, maintenance windows and shift-start notifications are few, cron-like, and must not run twice when there are several API replicas. That is Quartz's sweet spot (Postgres store + clustering). Start with a plain `BackgroundService` while there are one or two; adopt Quartz when running multiple replicas or when calendars are needed.

Escalation timers are not scheduled jobs: they are derived from DB state (alert fired at + step delay) and evaluated by a polling loop, which survives restarts without extra state.
