#!/usr/bin/env bash
# Fires every 60s via launchd. Resumes a paused cycle once its resume_at passes.
# Idempotent: bails fast if nothing to do.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"

[ -L "$ACTIVE_LINK" ] || exit 0
ACTIVE="$(readlink "$ACTIVE_LINK")"
STATE="${ART_ROOT}/${ACTIVE}/state.json"
[ -f "$STATE" ] || exit 0

PHASE=$(jq -r .phase "$STATE")
[ "$PHASE" = "paused" ] || exit 0

RESUME_AT=$(jq -r .resume_at "$STATE")
[ -n "$RESUME_AT" ] && [ "$RESUME_AT" != "null" ] || exit 0

NOW_EPOCH=$(date -u +%s)
# Parse ISO-8601 UTC → epoch (macOS BSD date)
RESUME_EPOCH=$(date -u -j -f "%Y-%m-%dT%H:%M:%SZ" "$RESUME_AT" +%s 2>/dev/null || echo 0)
[ "$RESUME_EPOCH" -gt 0 ] || exit 0

if [ "$NOW_EPOCH" -ge "$RESUME_EPOCH" ]; then
  log "watcher: resume_at $RESUME_AT reached; firing resume"
  export RUN_ID="$ACTIVE"
  exec "$(dirname "${BASH_SOURCE[0]}")/staging_loop.sh" --resume
fi

exit 0
