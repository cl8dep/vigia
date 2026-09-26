# Vision

## Problem

When something breaks, small teams without a dedicated SRE lose most of the time before the fix:

- Finding out it broke at all, ideally before users do.
- Knowing whether it is ours or a dependency's (DNS, CDN, a third-party provider).
- Waking up the right person, not everyone.
- Figuring out where to start: what changed, what the logs say, what else is failing.

Existing self-hosted tools cover pieces of this. Nobody covers detection, response and investigation together without SaaS or artificial limits.

## Audiences

Each audience plays a different role in the design:

| Audience | Role | Implication |
|---|---|---|
| Small and mid-size teams without SRE | Drives features | On-call, alert noise reduction, investigation help |
| Homelab / self-hosters | Drives install simplicity | Must run with one `docker run`, SQLite, low memory, Raspberry Pi |
| Orgs that cannot use SaaS | Drives architecture | No phone-home, local LLM, audit log, encrypted secrets from day one |

First real user: Flystern (travel platform with external providers such as Sabre).

## Principles

1. **Excellent without AI, better with it.** AI is an add-on layer. Every alert is delivered even if the LLM is down or not configured.
2. **The monitor lives outside the blast radius.** The control plane should not share infrastructure with what it monitors. Agents keep working and alerting when the control plane is unreachable.
3. **Outside-in and inside-out.** Agents run both in public regions and inside private networks. Comparing both views is the fastest path to a diagnosis.
4. **Everything is a plugin.** Checks, alert sources, notifiers, context providers and AI backends share one model: typed config, attributes, generated schema. See [plugins.md](plugins.md).
5. **Config as code.** Everything configurable in the UI is also declarable in YAML, with `plan` / `apply`.
6. **Integrate, don't replace.** Receive alerts from Prometheus, Grafana, Datadog. Do not store logs, metrics or traces.
7. **No artificial limits.** No team, user or alert caps in code.
8. **Read-only AI by default.** Any action suggested by AI requires human approval and cites its evidence.

## Non-goals

- APM, log storage, metrics storage, tracing.
- Native mobile apps (use ntfy, Telegram, etc.).
- Competing with Uptime Kuma on number of check types.
- License-key tiers in code.
