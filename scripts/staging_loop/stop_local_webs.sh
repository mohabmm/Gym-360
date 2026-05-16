#!/usr/bin/env bash
# Kill Next.js dev servers started by boot_local_webs.sh. Safe to run anytime.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"

[ -f "$WEB_PID_FILE" ] || { log "stop_local_webs: no PID file, nothing to stop"; exit 0; }

while read -r line; do
  case "$line" in
    external:*)
      log "stop_local_webs: $line — externally started, leaving alone"
      ;;
    [0-9]*)
      if kill -0 "$line" 2>/dev/null; then
        log "stop_local_webs: killing pid $line"
        # Kill process + descendants (next dev forks)
        pkill -P "$line" 2>/dev/null || true
        kill "$line" 2>/dev/null || true
        sleep 1
        kill -9 "$line" 2>/dev/null || true
      fi
      ;;
  esac
done < "$WEB_PID_FILE"

rm -f "$WEB_PID_FILE"
exit 0
