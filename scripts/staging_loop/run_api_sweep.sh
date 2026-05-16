#!/usr/bin/env bash
# Wrap scripts/ralph_smart_sweep.py against staging API.
# Output: artifacts/$RUN_ID/api/sweep.json + failures.json
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

ART_DIR="${ART_ROOT}/${RUN_ID}/api"
mkdir -p "$ART_DIR"

state_set sweeps.api.status running
state_set sweeps.api.started_at "$(date -u +%Y-%m-%dT%H:%M:%SZ)"

cd "$FORMA_ROOT"
LOG="$ART_DIR/sweep.log"
RESULT="$ART_DIR/sweep.json"
FAILURES="$ART_DIR/failures.json"

set +e
timeout "$TIMEOUT_API_SWEEP" python3 scripts/ralph_smart_sweep.py \
  --base "$STAGING_API" \
  --iters 1 \
  --out "$RESULT" \
  > "$LOG" 2>&1
RC=$?
set -e

# Parse failures (sweep emits JSON with .results[] containing .status)
if [ -f "$RESULT" ]; then
  jq '[.results[]? | select(.status >= 500 or .status == 0 or .ok == false)
       | {module: (.module // "unknown"),
          source: "api_sweep",
          endpoint: .endpoint,
          method: .method,
          status: .status,
          error: (.error // .body // "")}]' "$RESULT" > "$FAILURES" 2>/dev/null || echo "[]" > "$FAILURES"
else
  echo "[]" > "$FAILURES"
fi

COUNT=$(jq 'length' "$FAILURES")
state_set sweeps.api.status done
state_set sweeps.api.exit_code "$RC"
state_set sweeps.api.failures "$COUNT"
state_set sweeps.api.artifact "$RESULT"
log "api sweep done: $COUNT failures, exit=$RC"

# Copy failures to pending pool
[ "$COUNT" -gt 0 ] && cp "$FAILURES" "${PENDING_FAILURES}/${RUN_ID}_api.json"
exit 0
