#!/usr/bin/env bash
# Send a push notification to the user's Claude app via a tiny headless
# claude invocation using the PushNotification tool.
# Headless live-watch of the autonomous cycle is NOT supported in CLI 2.1.142,
# so push is how phone updates are delivered for automated runs.
#
# Usage: push_notify.sh "message text"
# No-op (logged) if ENABLE_PUSH_NOTIFY != 1 or claude missing.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"

MSG="${1:-}"
[ -n "$MSG" ] || exit 0

if [ "${ENABLE_PUSH_NOTIFY:-1}" != "1" ]; then
  log "push_notify disabled (ENABLE_PUSH_NOTIFY=${ENABLE_PUSH_NOTIFY:-1}); skipping: $MSG"
  exit 0
fi
if ! command -v "$CLAUDE_BIN" >/dev/null 2>&1; then
  log "push_notify: claude CLI absent; skipping: $MSG"
  exit 0
fi

# Escape double quotes for the prompt
SAFE="${MSG//\"/\'}"
set +e
timeout 90 "$CLAUDE_BIN" -p "Use the PushNotification tool exactly once to send this text to my Claude mobile app, then stop. Text: ${SAFE}" \
  --allowedTools "PushNotification" \
  --max-turns 6 \
  > "${LOG_ROOT}/push_notify.log" 2>&1
RC=$?
set -e
if [ "$RC" != "0" ]; then
  log "push_notify: claude exit $RC (see push_notify.log); message was: $MSG"
else
  log "push_notify sent: $MSG"
fi
exit 0
