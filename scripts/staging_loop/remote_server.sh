#!/usr/bin/env bash
# Persistent Claude Remote Control server, kept alive by launchd.
# Gives you an always-available live Forma session viewable in the Claude
# mobile/desktop app. Open it, then type a command (e.g. the staging-loop
# manual trigger) to run + watch live from your phone.
#
# The session URL / QR is written to test_logs/staging_loop/remote_url.log
# (StandardOutPath of the launchd job also captures it).
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"

cd "$FORMA_ROOT"
export CLAUDE_CONFIG_DIR

NAME="Forma staging-loop"
URLLOG="${LOG_ROOT}/remote_url.log"

log "remote_server: starting Remote Control server ($NAME)"
echo "=== remote-control started $(date -u +%Y-%m-%dT%H:%M:%SZ) ===" >> "$URLLOG"

# Provide a PTY via `script` (launchd has no controlling terminal; remote-control
# server expects a terminal to render the QR / URL). `script` keeps it alive
# and the server accepts network connections regardless.
# Workspace pre-trusted in ~/.claude-personal/.claude.json (.projects[path].
# hasTrustDialogAccepted=true) so the no-TTY trust dialog never blocks.
RC_ARGS=(remote-control --name "$NAME" --spawn=same-dir)
# Keep stdin open forever: `script` (PTY) otherwise gets immediate EOF with no
# controlling terminal and remote-control treats it as Ctrl-D / quit.
if command -v script >/dev/null 2>&1; then
  exec script -q /dev/null "$CLAUDE_BIN" "${RC_ARGS[@]}" < <(while :; do sleep 3600; done)
else
  exec "$CLAUDE_BIN" "${RC_ARGS[@]}" < <(while :; do sleep 3600; done)
fi
