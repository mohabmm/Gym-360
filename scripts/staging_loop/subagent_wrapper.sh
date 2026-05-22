#!/usr/bin/env bash
# Wraps a claude headless invocation. Detects quota errors, persists state.
# Not used directly when orchestrator spawns via Agent tool (Agent tool handles
# its own lifecycle); this is the CLI fallback for manual subagent runs +
# the path the driver uses to spawn the orchestrator itself.
#
# Usage: subagent_wrapper.sh <state_dump_path> <max_turns> <allowed_tools> <prompt_file>
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"

STATE_OUT="${1:?state out path}"
MAX_TURNS="${2:-30}"
TOOLS="${3:-Read,Edit,Write,Bash,Grep,Glob}"
PROMPT_FILE="${4:?prompt file}"

LOG="${STATE_OUT}.log"
mkdir -p "$(dirname "$STATE_OUT")"

set +e
"$CLAUDE_BIN" -p "$(cat "$PROMPT_FILE")" \
  --allowedTools "$TOOLS" \
  --max-turns "$MAX_TURNS" \
  > "$LOG" 2>&1
RC=$?
set -e

# Detect quota errors
RESUME=""
if grep -qiE 'rate.?limit|usage.?limit|quota|too many requests|429' "$LOG"; then
  RESUME=$(grep -oE 'reset.{0,20}[0-9T:Z+-]{8,}' "$LOG" | head -1 | grep -oE '[0-9T:Z+-]{8,}' || true)
  if [ -z "$RESUME" ]; then
    RESUME=$(grep -oE '"reset_at"[^,}]+' "$LOG" | head -1 | grep -oE '"[^"]+"$' | tr -d '"' || true)
  fi
  if [ -z "$RESUME" ]; then
    RESUME=$(date -u -v+${QUOTA_FALLBACK_HOURS}H +%Y-%m-%dT%H:%M:%SZ)
  fi
  cat > "$STATE_OUT" <<EOF
{"status":"paused","reason":"quota_hit","exit_code":$RC,"resume_at":"$RESUME","log":"$LOG"}
EOF
  echo "QUOTA HIT — resume_at=$RESUME"
  exit 42  # special code consumed by caller
fi

cat > "$STATE_OUT" <<EOF
{"status":"completed","exit_code":$RC,"log":"$LOG"}
EOF
exit "$RC"
