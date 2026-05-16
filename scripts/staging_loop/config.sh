#!/usr/bin/env bash
# Forma staging-loop shared config. Sourced by every script.
set -euo pipefail

# Repo
export FORMA_ROOT="/Users/mmagdy2-eg/Development/Forma"
export FORMA_DEV_BRANCH="develop"
export FORMA_PR_BASE="develop"

# Staging URLs (BE always remote; webs can be local — see WEB_MODE below)
export STAGING_API="https://staging-api.gym-365.com"
# All controllers are served under the /api/v1 route prefix (ASP.NET ApiVersioning).
# Sweep scripts must prepend this to every API path.
export API_PREFIX="${API_PREFIX:-/api/v1}"
export STAGING_ADMIN="https://staging-admin.gym-365.com"
export STAGING_STAFF="https://staging-staff.gym-365.com"

# Web testing mode: "local" boots Next.js dev servers pointing at STAGING_API,
# "staging" hits the deployed STAGING_ADMIN / STAGING_STAFF URLs directly.
# Default local since staging web DNS is unconfirmed.
export WEB_MODE="${WEB_MODE:-local}"
export LOCAL_ADMIN_URL="http://localhost:3003"
export LOCAL_STAFF_URL="http://localhost:3001"
export WEB_PID_FILE="/tmp/forma_staging_loop_webs.pids"

# Health endpoint (ASP.NET MapHealthChecks in src/API/GymApp.Api/Program.cs)
export STAGING_HEALTH_PATH="/health/live"

# Demo credentials (seeded; password = 123456)
export DEMO_PWD="123456"
export DEMO_ADMIN="admin@demo.local"
export DEMO_OWNER="owner@demo.local"
export DEMO_STAFF="staff@demo.local"
export DEMO_TRAINER="trainer@demo.local"
export DEMO_TRAINEE="trainee@demo.local"

# Paths
export ART_ROOT="${FORMA_ROOT}/artifacts"
export LOOP_ROOT="${FORMA_ROOT}/scripts/staging_loop"

# Portable tool shims first on PATH (provides `timeout` on macOS via gtimeout)
export PATH="${LOOP_ROOT}/bin:${PATH}"
export LOG_ROOT="${FORMA_ROOT}/test_logs/staging_loop"
export LOCK_FILE="/tmp/forma_staging_loop.lock"
export PID_FILE="/tmp/forma_staging_loop.pid"
export ACTIVE_LINK="${ART_ROOT}/_active_run_id"
export PENDING_FAILURES="${ART_ROOT}/_pending_failures"

# Timeouts (seconds)
export TIMEOUT_HEALTH=30
export TIMEOUT_DEPLOY_WAIT=300
export TIMEOUT_API_SWEEP=2400
export TIMEOUT_WEB_E2E=1800
export TIMEOUT_MOBILE=2400
export TIMEOUT_LOCAL_TESTS=3000
export TIMEOUT_VERIFY=3600
export TIMEOUT_MUTATION=14400
export TIMEOUT_SUBAGENT_HARD=2700  # 45min wall cap per subagent

# Concurrency
export MAX_PARALLEL_SUBAGENTS=4
export MAX_SUBAGENT_RETRIES=2
export MAX_SUBAGENT_EDITS=5

# Thresholds
export MUTATION_THRESHOLD_HIGH=75
export MUTATION_THRESHOLD_LOW=60
export MUTATION_THRESHOLD_BREAK=50
export PERF_REGRESSION_PCT=30  # warn-only first 30 days
export PERF_GATE_ENABLED=0     # 0=warn, 1=gate

# Quota fallback if reset time un-parseable
export QUOTA_FALLBACK_HOURS=5

# Sub-shell exec for claude headless.
# `claude` is a shell FUNCTION (account switcher) — unavailable under launchd.
# Use the real binary + pin the personal Max account config dir.
export CLAUDE_BIN="/Users/mmagdy2-eg/.local/bin/claude"
export CLAUDE_CONFIG_DIR="${CLAUDE_CONFIG_DIR:-$HOME/.claude-personal}"
export CLAUDE_ALLOWED_TOOLS_ORCHESTRATOR="Read,Bash,Grep,Glob,Agent,TodoWrite,PushNotification"
export CLAUDE_ALLOWED_TOOLS_SUBAGENT="Read,Edit,Write,Bash,Grep,Glob,TodoWrite"
export CLAUDE_MAX_TURNS_ORCHESTRATOR=120
export CLAUDE_MAX_TURNS_SUBAGENT=30

# Notifications
export NOTIFY_TITLE="Forma staging-loop"
export ENABLE_PUSH_NOTIFY=1   # phone push via PushNotification tool (headless live-watch unsupported in CLI 2.1.142)

# Stage flags (toggle phases on/off without code change)
export ENABLE_HEALTH=1
export ENABLE_DEPLOY_WAIT=1
export ENABLE_API_SWEEP=1
export ENABLE_WEB_E2E=1
export ENABLE_MOBILE=1
export ENABLE_MT_ISOLATION=1
export ENABLE_LOG_SCAN=1
export ENABLE_BASELINE_TESTS=1
export ENABLE_LOCAL_TESTS=1
export ENABLE_VERIFY=1
export ENABLE_PUSH=1

mkdir -p "$ART_ROOT" "$LOG_ROOT" "$PENDING_FAILURES"

log() {
  local msg="$*"
  local ts="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "[$ts] $msg" | tee -a "${LOG_ROOT}/loop.log"
}

die() {
  log "FATAL: $*"
  exit 1
}
