#!/usr/bin/env bash
# Atomic state.json read/write helpers.
# Usage: source state.sh; state_init <run_id>; state_set <jq_path> <value>; state_get <jq_path>
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"

state_file() {
  local run_id="${1:-${RUN_ID:-}}"
  [ -n "$run_id" ] || die "state_file: run_id missing"
  echo "${ART_ROOT}/${run_id}/state.json"
}

state_init() {
  local run_id="$1"
  local now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  local dir="${ART_ROOT}/${run_id}"
  mkdir -p "$dir/subagents"
  local f="$dir/state.json"
  if [ ! -f "$f" ]; then
    cat > "$f" <<EOF
{
  "run_id": "${run_id}",
  "phase": "init",
  "started_at": "${now}",
  "last_checkpoint_at": "${now}",
  "trigger": "${TRIGGER:-cron}",
  "health":          {"status": "pending"},
  "deploy_wait":     {"status": "pending"},
  "sweeps":          {
    "api":             {"status": "pending"},
    "web_admin":       {"status": "pending"},
    "web_staff":       {"status": "pending"},
    "mobile_trainee":  {"status": "pending"},
    "mobile_trainer":  {"status": "pending"},
    "mobile_staff":    {"status": "pending"},
    "mt_isolation":    {"status": "pending"},
    "log_scan":        {"status": "pending"}
  },
  "baseline_tests":  {"status": "pending"},
  "modules_to_fix":  [],
  "subagents":       {},
  "merged_branch":   null,
  "local_tests":     {"status": "pending"},
  "verify":          {"status": "pending"},
  "merge_conflicts": [],
  "uncertain":       [],
  "pr_url":          null,
  "paused_at":       null,
  "resume_at":       null,
  "pause_reason":    null,
  "pause_count":     0,
  "outcome":         null
}
EOF
  fi
}

state_set() {
  local path="$1"; shift
  local value="$*"
  local f
  f="$(state_file)"
  [ -f "$f" ] || die "state_set: $f missing"
  local tmp
  tmp="$(mktemp "${f}.XXXXXX")"
  # If value parses as JSON (object/array/number/bool/null) use --argjson, else --arg
  if echo "$value" | jq -e '.' >/dev/null 2>&1; then
    jq --argjson v "$value" ".${path} = \$v | .last_checkpoint_at = \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"" "$f" > "$tmp"
  else
    jq --arg v "$value" ".${path} = \$v | .last_checkpoint_at = \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"" "$f" > "$tmp"
  fi
  mv "$tmp" "$f"
}

state_get() {
  local path="$1"
  local f
  f="$(state_file)"
  jq -r ".${path} // empty" "$f"
}

state_phase() {
  state_get phase
}

state_set_phase() {
  state_set phase "$1"
  log "phase → $1 (run=${RUN_ID})"
}

state_set_outcome() {
  state_set outcome "$1"
}

state_pause() {
  local reason="$1"
  local resume_at="$2"
  local now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  state_set paused_at "$now"
  state_set resume_at "$resume_at"
  state_set pause_reason "$reason"
  local cnt
  cnt="$(state_get pause_count)"
  state_set pause_count "$((cnt + 1))"
  state_set_phase paused
  log "cycle paused: $reason; resume_at=$resume_at"
}

subagent_state_file() {
  local module="$1"
  echo "${ART_ROOT}/${RUN_ID}/subagents/${module}/state.json"
}

subagent_state_init() {
  local module="$1"
  local branch="$2"
  local worktree="$3"
  local dir="${ART_ROOT}/${RUN_ID}/subagents/${module}"
  mkdir -p "$dir"
  local f="$dir/state.json"
  cat > "$f" <<EOF
{
  "module": "${module}",
  "status": "in_progress",
  "started_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "branch": "${branch}",
  "worktree": "${worktree}",
  "commits": [],
  "files_touched": [],
  "rejected_attempts": 0,
  "reject_reasons": [],
  "last_step": "spawned"
}
EOF
}

subagent_state_set() {
  local module="$1"; local path="$2"; local value="$3"
  local f
  f="$(subagent_state_file "$module")"
  local tmp
  tmp="$(mktemp "${f}.XXXXXX")"
  if echo "$value" | jq -e '.' >/dev/null 2>&1; then
    jq --argjson v "$value" ".${path} = \$v" "$f" > "$tmp"
  else
    jq --arg v "$value" ".${path} = \$v" "$f" > "$tmp"
  fi
  mv "$tmp" "$f"
}
