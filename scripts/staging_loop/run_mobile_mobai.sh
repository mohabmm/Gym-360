#!/usr/bin/env bash
# MobAI mobile suite. Args: <trainee|trainer|staff>
# Requires: emulator running OR real device + MobAI bridge.
# Maestro flows live under gymapp-mobile/apps/<app>/.maestro/flows/ (yaml).
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

APP="${1:?usage: $0 <trainee|trainer|staff>}"
case "$APP" in
  trainee) STATE_KEY="mobile_trainee"; MOB_DIR="gymapp-mobile/apps/gymapp_trainee/.maestro/flows";;
  trainer) STATE_KEY="mobile_trainer"; MOB_DIR="gymapp-mobile/apps/gymapp_trainer/.maestro/flows";;
  staff)   STATE_KEY="mobile_staff";   MOB_DIR="gymapp-mobile/apps/gymapp_gym/.maestro/flows";;
  *) echo "invalid app: $APP"; exit 1;;
esac

ART_DIR="${ART_ROOT}/${RUN_ID}/mobile_${APP}"
mkdir -p "$ART_DIR"
state_set "sweeps.${STATE_KEY}.status" running

cd "$FORMA_ROOT"

if [ ! -d "$MOB_DIR" ]; then
  log "mobile $APP: $MOB_DIR not found, marking skipped"
  state_set "sweeps.${STATE_KEY}.status" skipped
  state_set "sweeps.${STATE_KEY}.reason" "no_scripts"
  echo "[]" > "$ART_DIR/failures.json"
  exit 0
fi

# Ensure MobAI bridge running (best-effort; if MobAI MCP available via claude headless,
# the orchestrator handles it; this CLI path is for direct .mob execution).
# Convention: maestro CLI executes .mob files. If unavailable, mark skipped.
if ! command -v maestro >/dev/null 2>&1; then
  log "mobile $APP: maestro CLI absent; marking skipped"
  state_set "sweeps.${STATE_KEY}.status" skipped
  state_set "sweeps.${STATE_KEY}.reason" "no_maestro_cli"
  echo "[]" > "$ART_DIR/failures.json"
  exit 0
fi

LOG="$ART_DIR/run.log"
RESULTS="$ART_DIR/results.xml"
FAIL_FILE="$ART_DIR/failures.json"

set +e
timeout "$TIMEOUT_MOBILE" maestro test "$MOB_DIR" \
  --format JUNIT --output "$RESULTS" \
  -e API_BASE="$STAGING_API" \
  > "$LOG" 2>&1
RC=$?
set -e

if [ -f "$RESULTS" ]; then
  # Parse junit XML → failures.json
  python3 - <<PY > "$FAIL_FILE"
import json, xml.etree.ElementTree as ET, sys
try:
    t = ET.parse("$RESULTS").getroot()
except Exception as e:
    print(json.dumps([{"module":"mobile-$APP","source":"mobile","error":f"junit parse failed: {e}"}]))
    sys.exit(0)
out = []
for tc in t.iter("testcase"):
    fail = tc.find("failure") if tc.find("failure") is not None else tc.find("error")
    if fail is not None:
        out.append({
            "module": "mobile-$APP",
            "source": "mobile",
            "name": tc.get("name"),
            "classname": tc.get("classname"),
            "error": (fail.text or "")[:2000]
        })
print(json.dumps(out))
PY
else
  echo "[{\"module\":\"mobile-${APP}\",\"source\":\"mobile\",\"error\":\"no junit produced\",\"exit\":$RC}]" > "$FAIL_FILE"
fi

COUNT=$(jq 'length' "$FAIL_FILE")
state_set "sweeps.${STATE_KEY}.status" done
state_set "sweeps.${STATE_KEY}.exit_code" "$RC"
state_set "sweeps.${STATE_KEY}.failures" "$COUNT"
state_set "sweeps.${STATE_KEY}.artifact" "$RESULTS"
log "mobile $APP done: $COUNT failures, exit=$RC"

[ "$COUNT" -gt 0 ] && cp "$FAIL_FILE" "${PENDING_FAILURES}/${RUN_ID}_mobile_${APP}.json"
exit 0
