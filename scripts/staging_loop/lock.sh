#!/usr/bin/env bash
# flock + PID guard. Source after config.sh.
# Usage: acquire_lock_or_resume "$@"  → sets RUN_ID, MODE=new|resume; exits if active
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

acquire_lock_or_resume() {
  exec 200>"$LOCK_FILE"
  if ! flock -n 200; then
    if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
      log "lock held by live pid $(cat "$PID_FILE"); exiting (no overlap)"
      exit 0
    fi
    log "lock orphan detected; forcibly taking lock"
    flock 200 || die "cannot acquire lock"
  fi
  echo $$ > "$PID_FILE"
  trap 'rm -f "$PID_FILE"; flock -u 200 2>/dev/null || true' EXIT INT TERM

  # Decide mode
  local active=""
  if [ -L "$ACTIVE_LINK" ]; then
    active="$(readlink "$ACTIVE_LINK")"
  fi
  if [ -n "$active" ] && [ -f "${ART_ROOT}/${active}/state.json" ]; then
    local phase
    phase="$(jq -r .phase "${ART_ROOT}/${active}/state.json")"
    if [ "$phase" != "done" ] && [ "$phase" != "failed" ] && [ "$phase" != "skipped" ]; then
      export RUN_ID="$active"
      export MODE="resume"
      log "resuming cycle RUN_ID=$RUN_ID phase=$phase"
      return 0
    fi
  fi

  # New cycle
  export RUN_ID="$(date -u +%Y%m%d-%H%M%S)"
  export MODE="new"
  state_init "$RUN_ID"
  ln -sfn "$RUN_ID" "$ACTIVE_LINK"
  log "started new cycle RUN_ID=$RUN_ID"
}

release_active_run() {
  rm -f "$ACTIVE_LINK"
}
