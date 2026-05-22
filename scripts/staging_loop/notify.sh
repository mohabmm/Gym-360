#!/usr/bin/env bash
# Mac desktop notification (free, native).
# Usage: notify "title" "message"
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"

notify() {
  local title="${1:-$NOTIFY_TITLE}"
  local msg="${2:-}"
  log "NOTIFY: $title — $msg"
  if command -v osascript >/dev/null 2>&1; then
    # Escape double quotes
    local safe_title="${title//\"/\\\"}"
    local safe_msg="${msg//\"/\\\"}"
    osascript -e "display notification \"$safe_msg\" with title \"$safe_title\"" || true
  fi
}

notify_cycle_done() {
  local run_id="$1"; local outcome="$2"; local pr_url="${3:-}"
  "$(dirname "${BASH_SOURCE[0]}")/push_notify.sh" "Forma loop $run_id finished: $outcome ${pr_url}" >/dev/null 2>&1 || true
  case "$outcome" in
    green)   notify "$NOTIFY_TITLE" "✅ cycle $run_id green; PR: $pr_url";;
    fixed)   notify "$NOTIFY_TITLE" "🔧 cycle $run_id auto-fixed; PR draft: $pr_url";;
    red)     notify "$NOTIFY_TITLE" "❌ cycle $run_id red; verify failed; branch kept local";;
    paused)  notify "$NOTIFY_TITLE" "⏸ cycle $run_id paused (quota); will resume automatically";;
    skipped) notify "$NOTIFY_TITLE" "⚠️ cycle $run_id skipped (staging down)";;
    *)       notify "$NOTIFY_TITLE" "cycle $run_id outcome=$outcome";;
  esac
}
