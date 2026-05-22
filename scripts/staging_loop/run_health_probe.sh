#!/usr/bin/env bash
# Pre-flight staging health probe. Fail fast if any target down.
# Exit 0 = all healthy, 2 = staging down (cycle skipped), 1 = script error
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

ART_DIR="${ART_ROOT}/${RUN_ID}/health"
mkdir -p "$ART_DIR"

state_set health.status running
state_set health.started_at "$(date -u +%Y-%m-%dT%H:%M:%SZ)"

probe() {
  local name="$1"; local url="$2"
  local code body
  body="$(mktemp)"
  code="$(curl -sS -o "$body" -w '%{http_code}' --max-time "$TIMEOUT_HEALTH" "$url" || echo 000)"
  echo "{\"name\":\"$name\",\"url\":\"$url\",\"http\":$code}" > "$ART_DIR/${name}.json"
  rm -f "$body"
  if [ "$code" = "200" ] || [ "$code" = "204" ]; then
    log "health $name OK ($code)"
    return 0
  fi
  log "health $name DOWN ($code)"
  return 1
}

FAIL=0
probe api    "${STAGING_API}${STAGING_HEALTH_PATH}"   || FAIL=1
if [ "${WEB_MODE:-local}" = "local" ]; then
  log "health: WEB_MODE=local, skipping remote admin/staff probes (will boot locally before sweep)"
else
  probe admin  "${STAGING_ADMIN}"             || FAIL=1
  probe staff  "${STAGING_STAFF}"             || FAIL=1
fi

if [ "$FAIL" = "1" ]; then
  state_set health.status failed
  state_set health.outcome down
  state_set_outcome skipped
  state_set_phase skipped
  exit 2
fi

state_set health.status done
state_set health.outcome up
exit 0
