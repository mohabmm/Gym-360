#!/usr/bin/env bash
# Start an INTERACTIVE Claude session with Remote Control, so you can watch +
# drive it live from the Claude mobile/desktop app.
#
# IMPORTANT — capability limits (Claude Code CLI 2.1.142):
#   * The AUTONOMOUS staging-loop uses headless `claude -p`, which exits
#     immediately and CANNOT be live-viewed remotely. There is no
#     --prompt/--initial-prompt for `claude remote-control`, and it needs a
#     TTY (launchd has none). True autonomous + live-watch is NOT supported.
#   * For the autonomous loop, phone visibility is delivered via push
#     notifications (see push_notify.sh) — a live feed of phase/outcome.
#   * THIS script is for when YOU want to interactively work on the repo and
#     watch/control from your phone. It opens a normal interactive session
#     with Remote Control enabled.
#
# Usage:
#   ./scripts/staging_loop/start_remote.sh [session-name]
#
# Then: scan the QR / open the printed claude.ai/code URL in the Claude app.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"

NAME="${1:-forma-$(date +%Y%m%d-%H%M)}"
cd "$FORMA_ROOT"

if ! command -v "$CLAUDE_BIN" >/dev/null 2>&1; then
  echo "claude CLI not found" >&2; exit 1
fi

echo "Starting interactive Claude session with Remote Control."
echo "Session name: $NAME"
echo "Working dir : $FORMA_ROOT"
echo
echo "When it starts: press SPACE to show the QR code, or copy the"
echo "claude.ai/code URL, and open it in the Claude mobile/desktop app."
echo "You can then watch + type from your phone."
echo

exec "$CLAUDE_BIN" remote-control --name "$NAME"
