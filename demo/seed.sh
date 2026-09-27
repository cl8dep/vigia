#!/usr/bin/env bash
# Seeds the demo: a user, checks, rules, services with dependencies and a public status page.
# Usage: ./demo/seed.sh [base-url]   (default http://localhost:8080)
set -euo pipefail

BASE="${1:-http://localhost:8080}"
EMAIL="demo@vigia.local"
PASSWORD='Demo!Passw0rd'

json() { python3 -c "import sys, json; print(json.load(sys.stdin)$1)"; }

call() {
  local method=$1 path=$2 body=${3:-}
  local response status
  response=$(curl -sS -w '\n%{http_code}' -X "$method" "$BASE$path" \
    -H 'Content-Type: application/json' ${TOKEN:+-H "Authorization: Bearer $TOKEN"} ${body:+-d "$body"})
  status=${response##*$'\n'}
  if [[ $status -ge 400 && $status -ne 409 ]]; then
    echo "$method $path -> $status: ${response%$'\n'*}" >&2
    exit 1
  fi
  echo "${response%$'\n'*}"
}

echo "Waiting for the API..."
# A proxied route: 404 (page not seeded yet) means the API answers; 502 means it is still starting.
until [[ $(curl -s -o /dev/null -w '%{http_code}' "$BASE/status/public") =~ ^(200|404)$ ]]; do sleep 1; done

call POST /api/v1/auth/sign-up "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" >/dev/null || true
TOKEN=$(call POST /api/v1/auth/sign-in "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" | json "['accessToken']")

check() { call POST /api/v1/checks "$1" >/dev/null && echo "  check $(echo "$1" | json "['slug']")"; }
echo "Checks"
check '{"slug":"booking-eu","name":"Booking API (EU)","plugin":"vigia.check.http","interval":"15s","config":{"url":"http://booking-eu/health"},"tags":{"env":"demo","service":"booking","region":"eu-west"}}'
check '{"slug":"booking-us","name":"Booking API (US)","plugin":"vigia.check.http","interval":"15s","config":{"url":"http://booking-us/health"},"tags":{"env":"demo","service":"booking","region":"us-east"}}'
check '{"slug":"sabre-api","name":"Sabre availability","plugin":"vigia.check.http","interval":"15s","config":{"url":"http://sabre/health"},"tags":{"env":"demo","service":"sabre","vigia:external":null}}'
check '{"slug":"website","name":"Website","plugin":"vigia.check.http","interval":"30s","config":{"url":"https://example.com"},"tags":{"env":"demo","service":"website"}}'
check '{"slug":"website-tls","name":"Website certificate","plugin":"vigia.check.tls","interval":"1h","config":{"host":"example.com"},"tags":{"env":"demo","service":"website"}}'
check '{"slug":"dns","name":"DNS resolution","plugin":"vigia.check.dns","interval":"30s","config":{"host":"example.com","resolvers":["1.1.1.1","8.8.8.8"]},"tags":{"env":"demo","service":"dns"}}'
check '{"slug":"nightly-batch","name":"Nightly batch","plugin":"vigia.check.heartbeat","interval":"15s","config":{"every":"1m","grace":"30s"},"tags":{"env":"demo","service":"batch"}}'

rule() { call POST /api/v1/rules "$1" >/dev/null && echo "  rule $(echo "$1" | json "['slug']")"; }
echo "Rules"
rule '{"slug":"down","selector":{"env":"demo"},"when":{"outcome":"down"},"for":2,"recoverAfter":1,"severity":"critical"}'
rule '{"slug":"slow-http","selector":{"env":"demo","vigia:plugin":"vigia.check.http"},"when":{"dimension":"latency","above":1500},"for":3,"severity":"warning"}'
rule '{"slug":"tls-expiry","selector":{"vigia:plugin":"vigia.check.tls"},"when":{"dimension":"days-to-expiry","below":14},"severity":"warning"}'

service() { call POST /api/v1/services "$1" >/dev/null && echo "  service $(echo "$1" | json "['slug']")"; }
echo "Services"
service '{"slug":"sabre","name":"Sabre","checks":{"service":"sabre"},"tags":{"vigia:external":null}}'
service '{"slug":"dns","name":"DNS","checks":{"service":"dns"},"tags":{"vigia:external":null}}'
service '{"slug":"booking","name":"Booking API","checks":{"service":"booking"},"partitionBy":"region","dependsOn":[{"service":"sabre","mode":"soft"}]}'
service '{"slug":"website","name":"Website","checks":{"service":"website"},"dependsOn":[{"service":"dns","mode":"blocking"}]}'
service '{"slug":"batch","name":"Nightly batch","checks":{"service":"batch"}}'
service '{"slug":"checkout","name":"Checkout","dependsOn":[{"service":"booking","mode":"blocking"},{"service":"website","mode":"blocking"}]}'

echo "Status page"
call POST /api/v1/status-pages '{
  "slug":"public",
  "title":"Flystern status",
  "description":"Real-time status of our services",
  "components":[
    {"service":"checkout","name":"Checkout","group":"Core"},
    {"service":"booking","name":"Bookings","group":"Core"},
    {"service":"website","name":"Website","group":"Core"},
    {"service":"sabre","name":"Flight search provider","group":"Providers"},
    {"service":"dns","name":"DNS","group":"Providers"},
    {"service":"batch","name":"Nightly processing","group":"Operations"}
  ]}' >/dev/null

TOKEN_HB=$(call POST /api/v1/checks/nightly-batch/webhook-token | json "['token']")
cat <<INFO

Done.
  Status page:  $BASE
  Public JSON:  $BASE/status/public
  API login:    $EMAIL / $PASSWORD

Try:
  docker compose -f demo/docker-compose.yml stop sabre       # provider down: Bookings degraded
  docker compose -f demo/docker-compose.yml stop booking-eu  # one region down: partial outage
  curl "$BASE/api/v1/hooks/nightly-batch/ping?token=$TOKEN_HB"   # heartbeat ping
INFO
