#!/usr/bin/env bash
# Phase dispatcher — called by driver after lock+state load.
# Jumps to the right phase based on state.json
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

[ -n "${RUN_ID:-}" ] || die "resume.sh: RUN_ID unset"

PHASE="$(state_get phase)"
log "dispatcher: phase=$PHASE"

DRV="$(dirname "${BASH_SOURCE[0]}")/staging_loop.sh"

case "$PHASE" in
  init|paused)        exec "$DRV" --phase health ;;
  health|skipped)     exec "$DRV" --phase health ;;
  deploy_wait)        exec "$DRV" --phase deploy_wait ;;
  sweeping)           exec "$DRV" --phase sweeping ;;
  baseline_tests)     exec "$DRV" --phase baseline_tests ;;
  triaged)            exec "$DRV" --phase fixing ;;
  fixing)             exec "$DRV" --phase fixing ;;
  merging)            exec "$DRV" --phase merging ;;
  local_tests)        exec "$DRV" --phase local_tests ;;
  verifying)          exec "$DRV" --phase verifying ;;
  pushing)            exec "$DRV" --phase pushing ;;
  done|failed)        log "phase=$PHASE — nothing to resume"; exit 0 ;;
  *) die "unknown phase: $PHASE" ;;
esac
