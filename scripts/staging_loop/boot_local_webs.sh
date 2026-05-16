#!/usr/bin/env bash
# Boot admin (:3003) + staff (:3001) Next.js dev servers locally via the
# npm/turbo monorepo at gymapp-web. .env.local already points
# NEXT_PUBLIC_API_URL at staging-api. PIDs → $WEB_PID_FILE.
# Idempotent: reuse if already serving.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"

WEB_ROOT="${FORMA_ROOT}/gymapp-web"
mkdir -p "$LOG_ROOT"
ADMIN_LOG="${LOG_ROOT}/web_admin.dev.log"
STAFF_LOG="${LOG_ROOT}/web_staff.dev.log"
: > "$WEB_PID_FILE"

if [ "${WEB_MODE:-local}" != "local" ]; then
  log "boot_local_webs: WEB_MODE=$WEB_MODE → skipping local boot"
  exit 0
fi

is_up() {
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' --max-time 4 "$1" 2>/dev/null || echo 000)
  case "$code" in 200|301|302|307|308) return 0;; *) return 1;; esac
}

# Ensure deps once (npm — project enforces npm@10, turbo workspaces)
if [ ! -d "${WEB_ROOT}/node_modules" ]; then
  log "web: installing deps (npm ci) — first run"
  ( cd "$WEB_ROOT" && timeout 600 npm ci > "${LOG_ROOT}/web_npm_install.log" 2>&1 ) \
    || { log "web: npm ci failed; see web_npm_install.log"; exit 1; }
fi

boot_one() {
  local app="$1" port="$2" script="$3" logfile="$4"
  local url="http://localhost:${port}"

  if is_up "$url"; then
    log "web $app already serving on $port — reusing (not managed by loop)"
    echo "external:$port" >> "$WEB_PID_FILE"
    return 0
  fi

  log "web $app: starting ($script) on $port"
  ( cd "$WEB_ROOT" && nohup npm run "$script" > "$logfile" 2>&1 & echo $! > "${logfile}.pid" )
  local pid; pid=$(cat "${logfile}.pid")
  echo "$pid" >> "$WEB_PID_FILE"

  local i
  for i in $(seq 1 60); do          # up to 120s (turbo+next cold start is slow)
    if is_up "$url"; then
      log "web $app ready on $port (pid=$pid, ${i}x2s)"
      return 0
    fi
    # bail early if the npm process already died
    if ! kill -0 "$pid" 2>/dev/null; then
      log "web $app: process $pid exited early; see $logfile"
      return 1
    fi
    sleep 2
  done
  log "web $app: timeout waiting for $url; see $logfile"
  return 1
}

FAIL=0
boot_one admin     3003 dev:admin     "$ADMIN_LOG" || FAIL=1
boot_one gym-staff 3001 dev:gym-staff "$STAFF_LOG" || FAIL=1
exit $FAIL
