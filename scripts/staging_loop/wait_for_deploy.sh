#!/usr/bin/env bash
# Wait until staging-api version matches local develop HEAD SHA (or first 7 chars).
# Best-effort: if /version endpoint absent, skip with warning (don't block cycle).
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

state_set deploy_wait.status running

LOCAL_SHA="$(cd "$FORMA_ROOT" && git rev-parse --short origin/${FORMA_DEV_BRANCH})"
state_set deploy_wait.target_sha "$LOCAL_SHA"

deadline=$(( $(date +%s) + TIMEOUT_DEPLOY_WAIT ))
ATTEMPTS=0
while [ "$(date +%s)" -lt "$deadline" ]; do
  ATTEMPTS=$((ATTEMPTS+1))
  REMOTE="$(curl -sS --max-time 10 "${STAGING_API}/version" 2>/dev/null | jq -r '.commit // .sha // empty' 2>/dev/null || true)"
  if [ -z "$REMOTE" ]; then
    log "deploy: /version endpoint absent or unparseable; skipping wait"
    state_set deploy_wait.status skipped
    state_set deploy_wait.reason "no_version_endpoint"
    exit 0
  fi
  REMOTE_SHORT="${REMOTE:0:7}"
  if [ "$REMOTE_SHORT" = "$LOCAL_SHA" ]; then
    log "deploy: staging at $REMOTE_SHORT matches local develop"
    state_set deploy_wait.status done
    state_set deploy_wait.remote_sha "$REMOTE_SHORT"
    state_set deploy_wait.attempts "$ATTEMPTS"
    exit 0
  fi
  log "deploy: staging=$REMOTE_SHORT local=$LOCAL_SHA; waiting..."
  sleep 15
done

log "deploy: timeout after ${TIMEOUT_DEPLOY_WAIT}s; proceeding anyway"
state_set deploy_wait.status timeout
state_set deploy_wait.attempts "$ATTEMPTS"
exit 0
