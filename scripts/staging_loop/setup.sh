#!/usr/bin/env bash
# One-shot installer. Idempotent. Run after every config change.
#   ./scripts/staging_loop/setup.sh        — install + load launchd jobs
#   ./scripts/staging_loop/setup.sh unload — unload launchd jobs
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"

PLISTS=(
  "com.forma.staging-loop"
  "com.forma.staging-loop-watcher"
  "com.forma.mutation-test"
  "com.forma.remote-control"
)

unload_all() {
  for p in "${PLISTS[@]}"; do
    if launchctl list "$p" >/dev/null 2>&1; then
      launchctl unload "$HOME/Library/LaunchAgents/${p}.plist" 2>/dev/null || true
      echo "unloaded $p"
    fi
  done
}

if [ "${1:-}" = "unload" ]; then
  unload_all
  exit 0
fi

# Dependency checks
echo "Checking dependencies..."
MISSING=0
for cmd in jq python3 git gh dotnet docker curl flock osascript pnpm node claude; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "  MISSING: $cmd"
    MISSING=1
  fi
done
if ! command -v maestro >/dev/null 2>&1; then
  echo "  WARN: maestro CLI absent — mobile sweeps will be marked skipped"
fi
[ "$MISSING" = "1" ] && { echo "install missing deps first (brew/dotnet/etc)"; exit 1; }
echo "  OK"

# Verify develop branch
cd "$FORMA_ROOT"
if ! git rev-parse --verify "$FORMA_DEV_BRANCH" >/dev/null 2>&1; then
  echo "branch '$FORMA_DEV_BRANCH' missing — creating from main"
  git checkout main
  git pull --ff-only origin main
  git checkout -b "$FORMA_DEV_BRANCH"
  git push -u origin "$FORMA_DEV_BRANCH"
fi
echo "develop branch: OK"

# Verify gh authenticated
if ! gh auth status >/dev/null 2>&1; then
  echo "gh not authenticated. Run: gh auth login"
  exit 1
fi
echo "gh auth: OK"

# Verify stryker tool
if ! dotnet tool list -g | grep -q dotnet-stryker; then
  echo "installing dotnet-stryker globally"
  dotnet tool install -g dotnet-stryker
fi
echo "stryker: OK"

# Verify Playwright browsers
if [ -d "${FORMA_ROOT}/gym-360-web/node_modules/playwright" ]; then
  (cd "${FORMA_ROOT}/gym-360-web" && npx playwright install --with-deps chromium 2>&1 | tail -3) || true
fi

# Make logs dir
mkdir -p "$LOG_ROOT" "$ART_ROOT" "$PENDING_FAILURES" "${ART_ROOT}/mutation"

# Install plists into ~/Library/LaunchAgents (symlink)
mkdir -p "$HOME/Library/LaunchAgents"
for p in "${PLISTS[@]}"; do
  ln -sf "$HERE/launchd/${p}.plist" "$HOME/Library/LaunchAgents/${p}.plist"
done

# Load (unload first to refresh)
unload_all
for p in "${PLISTS[@]}"; do
  launchctl load "$HOME/Library/LaunchAgents/${p}.plist"
  echo "loaded $p"
done

echo
echo "Setup complete."
echo "  Cron times (Cairo UTC+3): 19:00, 00:00, 05:00"
echo "  Watcher: every 60s"
echo "  Mutation: daily 08:00 Cairo"
echo
echo "Manual trigger: $HERE/staging_loop.sh --manual"
echo "Inspect:        tail -f $LOG_ROOT/loop.log"
echo "Unload all:     $HERE/setup.sh unload"
