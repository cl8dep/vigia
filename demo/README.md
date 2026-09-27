# Demo

Vigia with Postgres, a public status page, and stand-in targets you can break.

```bash
docker compose -f demo/docker-compose.yml up -d --build
./demo/seed.sh
open http://localhost:8080
```

`seed.sh` creates a user (`demo@vigia.local` / `Demo!Passw0rd`), checks (HTTP, TLS, DNS, heartbeat), rules, services with dependencies and a partitioned service, and the `public` status page.

| Service | Checks | Notes |
|---|---|---|
| Checkout | none | Business service: `blocking` on Bookings and Website |
| Bookings | `booking-eu`, `booking-us` | `partitionBy: region`; `soft` on Sabre |
| Website | `https://example.com` (HTTP, TLS) | `blocking` on DNS |
| Sabre | `http://sabre/health` | `vigia:external` |
| DNS | `example.com` via 1.1.1.1 and 8.8.8.8 | `vigia:external` |
| Nightly batch | heartbeat, every 1m, grace 30s | Goes down after 90s without a ping |

Break things and watch the page (rules fire after two failed probes, 15s apart):

```bash
docker compose -f demo/docker-compose.yml stop sabre        # provider down; Bookings degraded
docker compose -f demo/docker-compose.yml stop booking-eu   # one region down; Bookings and Checkout partial outage
docker compose -f demo/docker-compose.yml start sabre booking-eu
curl "http://localhost:8080/api/v1/hooks/nightly-batch/ping?token=<token printed by seed.sh>"
```

Public JSON: `http://localhost:8080/status/public`. Tear down with `docker compose -f demo/docker-compose.yml down -v`.

The status page in `status-page/` is adapted from Piro and licensed AGPL-3.0; see `status-page/NOTICE.md`.
