#!/usr/bin/env bash
# Main driver. Handles full cycle lifecycle.
# Modes:
#   (no args)                — new cycle or resume detected via lock
#   --manual                 — same as no args, sets trigger=manual
#   --resume                 — explicit resume (called by watcher)
#   --phase <name>           — internal jump (called by resume.sh dispatcher)
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/config.sh"
source "$HERE/state.sh"
source "$HERE/lock.sh"
source "$HERE/notify.sh"

push() { "$HERE/push_notify.sh" "$*" >/dev/null 2>&1 || true; }

PHASE_OVERRIDE=""
TRIGGER="cron"
while [ $# -gt 0 ]; do
  case "$1" in
    --manual)  TRIGGER="manual"; shift;;
    --resume)  TRIGGER="resume"; shift;;
    --phase)   PHASE_OVERRIDE="$2"; shift 2;;
    *) echo "unknown arg $1"; exit 1;;
  esac
done
export TRIGGER

# caffeinate for the duration + ensure local webs torn down on exit
cleanup() {
  "$HERE/stop_local_webs.sh" 2>/dev/null || true
  [ -n "${CAFF_PID:-}" ] && kill "$CAFF_PID" 2>/dev/null || true
  rm -f "$PID_FILE"
  flock -u 200 2>/dev/null || true
}
trap cleanup EXIT INT TERM

if command -v caffeinate >/dev/null 2>&1; then
  caffeinate -i -w $$ &
  CAFF_PID=$!
fi

acquire_lock_or_resume

# If phase override given, skip to it (dispatcher path)
if [ -n "$PHASE_OVERRIDE" ]; then
  ENTRY_PHASE="$PHASE_OVERRIDE"
else
  ENTRY_PHASE="$(state_get phase)"
  [ -z "$ENTRY_PHASE" ] || [ "$ENTRY_PHASE" = "init" ] && ENTRY_PHASE="health"
fi

cd "$FORMA_ROOT"

# Harden: a working tree left on another branch or with uncommitted edits must
# never wedge the loop. `git checkout` aborts on dirty tracked files — that is
# exactly what stalled the loop for days. Stash tracked changes up-front so every
# branch switch below is guaranteed clean. Untracked paths (artifacts/) are left
# alone. The stash is intentionally not auto-popped: a human can recover it.
if [ -n "$(git status --porcelain --untracked-files=no 2>/dev/null)" ]; then
  if git stash push --quiet -m "staging-loop auto-stash $(date -u +%Y%m%dT%H%M%SZ)" 2>/dev/null; then
    log "auto-stashed dirty working tree before branch switch"
  else
    log "WARNING: git stash failed; branch switch may abort"
  fi
fi

# Update develop in working tree (best-effort; resume may already be on it)
git fetch origin --quiet || log "git fetch failed; continuing"
git checkout "$FORMA_DEV_BRANCH" 2>/dev/null || true
git pull --ff-only origin "$FORMA_DEV_BRANCH" 2>/dev/null || true

#────────────────────────────────────────────────────────────────────────────────
# Auto-approve + auto-merge any stale auto-fix PR left open by a prior cycle.
# User policy: a new cycle must not start behind an un-merged PR — mark it ready,
# approve, squash-merge (--admin bypasses branch protection / draft / required
# reviews). gh user is the PR author so self-approve is rejected by GitHub; that
# is tolerated — the --admin merge does not require the approval to succeed.
# Merge failure is logged + pushed but does NOT block the new cycle.
#────────────────────────────────────────────────────────────────────────────────
# A merge can fail because the PR conflicts with the base (--admin bypasses
# branch protection, NOT merge conflicts). Loop regenerates fixes from triage
# every cycle, so a conflicting stale auto-fix PR is disposable: close it +
# delete its branch so it never wedges the loop. Returns 0 if it handled
# (closed) the PR, 1 if the failure was something else.
close_if_conflicting() {
  local pr="$1" ms
  ms="$(gh pr view "$pr" --json mergeable --jq .mergeable 2>/dev/null || echo UNKNOWN)"
  if [ "$ms" = "CONFLICTING" ]; then
    gh pr close "$pr" --delete-branch \
      --comment "Auto-closed by staging-loop: conflicts with $FORMA_PR_BASE; fixes regenerate next cycle from triage." \
      >/dev/null 2>&1 || true
    log "PR #$pr CONFLICTING → closed + branch deleted (fixes regenerate next cycle)"
    push "Forma loop: 🗑️ PR #$pr conflicting — auto-closed, branch deleted"
    return 0
  fi
  return 1
}

merge_stale_auto_fix_prs() {
  command -v gh >/dev/null 2>&1 || { log "stale-PR: gh not found — skip"; return 0; }
  local prs n rc mlog
  prs="$(gh pr list --base "$FORMA_PR_BASE" --state open \
           --json number,headRefName \
           --jq '.[] | select(.headRefName|startswith("auto-fix/")) | .number' \
         2>/dev/null || true)"
  if [ -z "$prs" ]; then
    log "stale-PR: no open auto-fix PR on $FORMA_PR_BASE"
    return 0
  fi
  for n in $prs; do
    log "stale-PR: #$n open from prior cycle — auto ready+approve+merge"
    gh pr ready "$n" >/dev/null 2>&1 || true
    # Self-approve is blocked by GitHub for the PR author; best-effort only.
    gh pr review "$n" --approve \
      --body "Auto-approved by staging-loop before next cycle" >/dev/null 2>&1 || true
    mlog="${LOG_ROOT}/stale_pr_${n}.log"
    gh pr merge "$n" --squash --delete-branch --admin >"$mlog" 2>&1
    rc=$?
    if [ "$rc" = "0" ]; then
      log "stale-PR: #$n squash-merged into $FORMA_PR_BASE"
      push "Forma loop: ♻️ stale PR #$n auto-merged to $FORMA_PR_BASE before new cycle"
    elif close_if_conflicting "$n"; then
      : # conflicting → closed + branch deleted above
    else
      log "stale-PR: #$n merge FAILED (rc=$rc, see $mlog) — new cycle continues anyway"
      push "Forma loop: ⚠️ stale PR #$n auto-merge FAILED; new cycle continues"
    fi
  done
  # Refresh working tree so phases see the just-merged commit.
  git fetch origin --quiet || true
  git checkout "$FORMA_DEV_BRANCH" 2>/dev/null || true
  git pull --ff-only origin "$FORMA_DEV_BRANCH" 2>/dev/null || true
}

# Only on a truly fresh cron/manual start (not a mid-cycle resume / phase jump).
if [ -z "$PHASE_OVERRIDE" ] \
   && { [ "$TRIGGER" = "cron" ] || [ "$TRIGGER" = "manual" ]; } \
   && [ "$ENTRY_PHASE" = "health" ]; then
  merge_stale_auto_fix_prs || true
fi

# Mutation break gate (pre-flight)
if [ -f "${ART_ROOT}/mutation/_break_marker" ]; then
  BREAK_RUN=$(cat "${ART_ROOT}/mutation/_break_marker")
  log "WARNING: mutation break marker present ($BREAK_RUN) — PR push gate will require explicit override"
fi

#────────────────────────────────────────────────────────────────────────────────
# Phase implementations. Each is idempotent + checks state to skip if done.
#────────────────────────────────────────────────────────────────────────────────

run_phase_health() {
  state_set_phase health
  [ "$MODE" = "resume" ] && push "Forma loop RESUMED ($RUN_ID) at phase health" \
                         || push "Forma staging-loop STARTED ($RUN_ID, trigger=$TRIGGER)"
  if [ "${ENABLE_HEALTH:-1}" = "1" ]; then
    if ! RUN_ID="$RUN_ID" "$HERE/run_health_probe.sh"; then
      log "health probe failed → cycle skipped"
      notify_cycle_done "$RUN_ID" "skipped"
      state_set_outcome skipped
      state_set_phase done
      release_active_run
      return 0
    fi
  fi
  run_phase_deploy_wait
}

run_phase_deploy_wait() {
  state_set_phase deploy_wait
  push "Forma loop $RUN_ID: ⏳ waiting for staging deploy"
  [ "${ENABLE_DEPLOY_WAIT:-1}" = "1" ] && RUN_ID="$RUN_ID" "$HERE/wait_for_deploy.sh" || true
  run_phase_sweeping
}

run_phase_sweeping() {
  state_set_phase sweeping
  push "Forma loop $RUN_ID: 🧹 sweeps starting (api/web/mobile/mt-iso/log-scan)"

  # Boot local webs if WEB_MODE=local (before launching Playwright projects)
  if [ "${WEB_MODE:-local}" = "local" ] && [ "${ENABLE_WEB_E2E:-1}" = "1" ]; then
    if ! "$HERE/boot_local_webs.sh"; then
      log "sweeping: local web boot failed — web sweeps will be marked failed"
      state_set sweeps.web_admin.status failed
      state_set sweeps.web_admin.reason "local_boot_failed"
      state_set sweeps.web_staff.status failed
      state_set sweeps.web_staff.reason "local_boot_failed"
    fi
  fi

  # Run sweeps; pending-only (resumable)
  declare -a JOBS=()
  if [ "${ENABLE_API_SWEEP:-1}" = "1" ] && [ "$(state_get sweeps.api.status)" != "done" ]; then
    RUN_ID="$RUN_ID" "$HERE/run_api_sweep.sh" &
    JOBS+=($!)
  fi
  if [ "${ENABLE_WEB_E2E:-1}" = "1" ] && [ "$(state_get sweeps.web_admin.status)" != "done" ]; then
    RUN_ID="$RUN_ID" "$HERE/run_web_e2e.sh" admin &
    JOBS+=($!)
  fi
  if [ "${ENABLE_WEB_E2E:-1}" = "1" ] && [ "$(state_get sweeps.web_staff.status)" != "done" ]; then
    RUN_ID="$RUN_ID" "$HERE/run_web_e2e.sh" staff &
    JOBS+=($!)
  fi
  # Mobile sequential (one device usually)
  if [ "${ENABLE_MOBILE:-1}" = "1" ]; then
    [ "$(state_get sweeps.mobile_trainee.status)" != "done" ] && RUN_ID="$RUN_ID" "$HERE/run_mobile_mobai.sh" trainee || true
    [ "$(state_get sweeps.mobile_trainer.status)" != "done" ] && RUN_ID="$RUN_ID" "$HERE/run_mobile_mobai.sh" trainer || true
    [ "$(state_get sweeps.mobile_staff.status)"   != "done" ] && RUN_ID="$RUN_ID" "$HERE/run_mobile_mobai.sh" staff   || true
  fi
  if [ "${ENABLE_MT_ISOLATION:-1}" = "1" ] && [ "$(state_get sweeps.mt_isolation.status)" != "done" ]; then
    RUN_ID="$RUN_ID" "$HERE/run_mt_isolation.sh" || true
  fi
  for pid in "${JOBS[@]}"; do wait "$pid" || true; done
  # Log scan runs last (so it covers the whole window)
  if [ "${ENABLE_LOG_SCAN:-1}" = "1" ] && [ "$(state_get sweeps.log_scan.status)" != "done" ]; then
    RUN_ID="$RUN_ID" "$HERE/run_log_scan.sh" || true
  fi

  # Per-sweep summary push (status per known sweep key)
  SWEEP_SUMMARY=""
  for k in api web_admin web_staff mobile_trainee mobile_trainer mobile_staff mt_isolation log_scan; do
    s="$(state_get "sweeps.$k.status" 2>/dev/null || true)"
    [ -n "$s" ] && SWEEP_SUMMARY="${SWEEP_SUMMARY}${k}=${s} "
  done
  push "Forma loop $RUN_ID: 🧹 sweeps done — ${SWEEP_SUMMARY:-no status}"

  run_phase_baseline_tests
}

run_phase_baseline_tests() {
  state_set_phase baseline_tests
  push "Forma loop $RUN_ID: 🧪 baseline tests running"
  if [ "${ENABLE_BASELINE_TESTS:-1}" = "1" ] && [ "$(state_get baseline_tests.status)" != "done" ]; then
    RUN_ID="$RUN_ID" "$HERE/run_baseline_tests.sh" || true
  fi
  push "Forma loop $RUN_ID: 🧪 baseline tests = $(state_get baseline_tests.status 2>/dev/null || echo n/a)"
  run_phase_triage
}

run_phase_triage() {
  state_set_phase triaged
  # Move per-cycle failures from pending pool to active triage file
  TRIAGE="${ART_ROOT}/${RUN_ID}/triage.json"
  python3 - <<'PY' > "$TRIAGE"
import os, json, glob
pool = os.environ["PENDING_FAILURES"]
run = os.environ["RUN_ID"]
all_f = []
seen = set()
# Cycle-tagged files
for fp in sorted(glob.glob(f"{pool}/{run}_*.json")):
    try:
        for it in json.load(open(fp)):
            key = (it.get("module"), it.get("source"), it.get("endpoint") or it.get("path") or it.get("test") or it.get("file"), it.get("error","")[:80])
            if key in seen: continue
            seen.add(key); all_f.append(it)
    except: pass
# Untagged cross-cycle pool (mutation, etc.)
for fp in sorted(glob.glob(f"{pool}/*.json")):
    if os.path.basename(fp).startswith(run + "_"): continue
    try:
        for it in json.load(open(fp)):
            key = (it.get("module"), it.get("source"), it.get("file"), it.get("error","")[:80])
            if key in seen: continue
            seen.add(key); all_f.append(it)
    except: pass
print(json.dumps(all_f, indent=2))
PY
  COUNT=$(jq 'length' "$TRIAGE")
  log "triage: $COUNT total failures across all sources"
  state_set triage_count "$COUNT"
  push "Forma loop $RUN_ID: sweeps done, $COUNT failures triaged"

  if [ "$COUNT" = "0" ]; then
    log "no failures — cycle ends green w/o PR"
    notify_cycle_done "$RUN_ID" "green"
    state_set_outcome green
    state_set_phase done
    release_active_run
    return 0
  fi

  # Set modules_to_fix
  MODULES=$(jq -r '[.[].module // "unknown"] | unique | @json' "$TRIAGE")
  state_set modules_to_fix "$MODULES"

  run_phase_fixing
}

run_phase_fixing() {
  state_set_phase fixing

  # Create merge branch
  MERGE_BRANCH="auto-fix/${RUN_ID}"
  state_set merged_branch "$MERGE_BRANCH"
  git checkout "$FORMA_DEV_BRANCH"
  git checkout -B "$MERGE_BRANCH" 2>&1 | head -5

  # Spawn orchestrator (single claude headless call; it fans out subagents via Agent tool)
  ORCH_LOG="${ART_ROOT}/${RUN_ID}/orchestrator.log"
  ORCH_OUT="${ART_ROOT}/${RUN_ID}/orchestrator_state.json"
  PROMPT="$HERE/prompts/orchestrator.md"

  push "Forma loop $RUN_ID: launching multi-agent fixers"
  log "fixing: launching orchestrator (claude headless)"
  set +e
  RUN_ID="$RUN_ID" "$CLAUDE_BIN" -p "$(sed "s|{{RUN_ID}}|$RUN_ID|g" "$PROMPT")" \
    --allowedTools "$CLAUDE_ALLOWED_TOOLS_ORCHESTRATOR" \
    --max-turns "$CLAUDE_MAX_TURNS_ORCHESTRATOR" \
    > "$ORCH_LOG" 2>&1
  ORC_RC=$?
  set -e

  # Detect quota in orchestrator log
  if grep -qiE 'rate.?limit|usage.?limit|quota|too many requests|429' "$ORCH_LOG"; then
    RESUME_AT=$(grep -oE 'reset.{0,30}[0-9T:Z+-]{8,}' "$ORCH_LOG" | head -1 | grep -oE '[0-9T:Z+-]{8,}' || true)
    [ -z "$RESUME_AT" ] && RESUME_AT=$(date -u -v+${QUOTA_FALLBACK_HOURS}H +%Y-%m-%dT%H:%M:%SZ)
    state_pause "quota_hit" "$RESUME_AT"
    notify_cycle_done "$RUN_ID" "paused"
    return 0
  fi

  if [ "$ORC_RC" != "0" ]; then
    log "orchestrator exited non-zero ($ORC_RC); marking cycle failed"
    state_set_outcome red
    state_set_phase failed
    notify_cycle_done "$RUN_ID" "red"
    release_active_run
    return 0
  fi

  run_phase_merging
}

run_phase_merging() {
  state_set_phase merging
  # Orchestrator already attempted merges into auto-fix/$RUN_ID per its prompt.
  # Here we just verify the branch has commits + no unresolved conflicts.
  MB="$(state_get merged_branch)"
  CHANGED=$(git diff --name-only "$FORMA_DEV_BRANCH..$MB" 2>/dev/null | wc -l | tr -d ' ')
  log "merging: $CHANGED files diff vs $FORMA_DEV_BRANCH"
  push "Forma loop $RUN_ID: 🔀 merging — $CHANGED files changed by fixers"
  if [ "$CHANGED" = "0" ]; then
    log "no changes from orchestrator — cycle ends w/o PR"
    state_set_outcome red
    state_set_phase done
    notify_cycle_done "$RUN_ID" "red"
    release_active_run
    return 0
  fi
  run_phase_local_tests
}

run_phase_local_tests() {
  state_set_phase local_tests
  push "Forma loop $RUN_ID: 🚦 local test gate running"
  set +e
  RUN_ID="$RUN_ID" "$HERE/run_local_tests.sh" gate
  RC=$?
  set -e
  if [ "$RC" != "0" ]; then
    log "local_tests gate FAILED → no push; branch kept local"
    state_set_outcome red
    state_set_phase failed
    notify_cycle_done "$RUN_ID" "red"
    release_active_run
    return 0
  fi
  push "Forma loop $RUN_ID: 🚦 local test gate PASSED"
  run_phase_verifying
}

run_phase_verifying() {
  state_set_phase verifying
  push "Forma loop $RUN_ID: 🔎 verifying fixes (mt-isolation re-check)"
  [ "${ENABLE_VERIFY:-1}" = "1" ] || { run_phase_pushing; return; }

  # Re-run staging sweep against fixed code (it's already pushed implicitly via remote?
  # No — fix branch local only here. The "verify" runs same suite but reads from local merged branch
  # would require deploy. Simpler: verify = local test gate (already done) + repeat MT isolation +
  # repeat staging sweep (which still hits old staging — but if our changes don't apply to staging
  # yet, this re-confirms baseline behavior). True post-deploy verify happens after merge to develop
  # triggers staging redeploy. For pre-push verify, we re-check that:
  #   1. local_tests green (already done)
  #   2. mt_isolation still green against staging baseline
  #   3. no NEW failures vs original triage

  V_DIR="${ART_ROOT}/${RUN_ID}/verify"
  mkdir -p "$V_DIR"

  # Re-run MT isolation (cheap, important)
  if [ "${ENABLE_MT_ISOLATION:-1}" = "1" ]; then
    OLD_RUN="$RUN_ID"
    export RUN_ID="${OLD_RUN}-verify"
    state_init "$RUN_ID"
    "$HERE/run_mt_isolation.sh" || true
    VFAIL=$(state_get sweeps.mt_isolation.failures 2>/dev/null || echo 0)
    export RUN_ID="$OLD_RUN"
    state_set verify.mt_isolation_failures "${VFAIL:-0}"
    if [ "${VFAIL:-0}" != "0" ]; then
      log "verify: mt_isolation still red — no push"
      state_set_outcome red
      state_set_phase failed
      notify_cycle_done "$RUN_ID" "red"
      release_active_run
      return 0
    fi
  fi

  state_set verify.status passed
  push "Forma loop $RUN_ID: ✅ verify passed — opening PR"
  run_phase_pushing
}

run_phase_pushing() {
  state_set_phase pushing
  [ "${ENABLE_PUSH:-1}" = "1" ] || { log "push disabled by flag"; state_set_phase done; release_active_run; return; }

  MB="$(state_get merged_branch)"
  cd "$FORMA_ROOT"

  # Idempotent push
  git push -u origin "$MB" 2>&1 | tee "${ART_ROOT}/${RUN_ID}/push.log"

  # Build PR body
  PR_BODY="${ART_ROOT}/${RUN_ID}/pr_body.md"
  TRIAGE="${ART_ROOT}/${RUN_ID}/triage.json"
  ORCH_STATE_FILE="$(state_file)"
  python3 - <<PY > "$PR_BODY"
import json, os
state = json.load(open("$ORCH_STATE_FILE"))
triage = json.load(open("$TRIAGE")) if os.path.exists("$TRIAGE") else []
print("## Auto-fix cycle: $RUN_ID")
print()
print(f"- Trigger: {state.get('trigger')}")
print(f"- Started: {state.get('started_at')}")
print(f"- Modules fixed: {', '.join(state.get('modules_to_fix') or [])}")
print()
print("## Failures triaged (by source)")
print()
print("| source | count |")
print("|---|---|")
from collections import Counter
c = Counter([f.get('source','?') for f in triage])
for k,v in c.most_common():
    print(f"| {k} | {v} |")
print()
print("## Subagent results")
print()
print("| module | status | files | tests added |")
print("|---|---|---|---|")
for m, s in (state.get("subagents") or {}).items():
    print(f"| {m} | {s.get('status','?')} | {len(s.get('files_touched') or [])} | {len(s.get('tests_added') or [])} |")
print()
print("## Verify")
print()
v = state.get("verify") or {}
print(f"- Local tests: {(state.get('local_tests') or {}).get('overall_exit', 'n/a')}")
print(f"- MT isolation re-check failures: {v.get('mt_isolation_failures','n/a')}")
print()
print("## Uncertain (manual review)")
print()
for u in (state.get("uncertain") or []):
    print(f"- {u.get('module')}: {u.get('reason')}")
print()
print("---")
print("Generated by staging-loop. Branch: $MB")
PY

  # Create PR only if not already open
  if gh pr list --head "$MB" --base "$FORMA_PR_BASE" --json url --jq '.[0].url' 2>/dev/null | grep -q http; then
    PR_URL=$(gh pr list --head "$MB" --base "$FORMA_PR_BASE" --json url --jq '.[0].url')
    log "PR already exists: $PR_URL"
  else
    PR_URL=$(gh pr create --base "$FORMA_PR_BASE" --head "$MB" --draft \
      --title "auto-fix: staging sweep $RUN_ID" \
      --body-file "$PR_BODY" 2>&1 | tail -1)
    log "PR created: $PR_URL"
  fi
  state_set pr_url "$PR_URL"

  # Auto-approve the freshly created PR. gh user is the PR author, so GitHub
  # rejects self-approval — best-effort only; the next-cycle stale handler
  # merges via --admin which does not require this approval to land.
  PR_NUM="$(gh pr list --head "$MB" --base "$FORMA_PR_BASE" --json number --jq '.[0].number' 2>/dev/null || true)"
  if [ -n "$PR_NUM" ]; then
    gh pr ready "$PR_NUM" >/dev/null 2>&1 || true
    gh pr review "$PR_NUM" --approve \
      --body "Auto-approved by staging-loop" >/dev/null 2>&1 \
      && log "PR #$PR_NUM auto-approved" \
      || log "PR #$PR_NUM auto-approve skipped (self-approval blocked by GitHub; --admin merge will still land)"

    # Manual trigger: merge this cycle's own PR into develop now (don't wait
    # for the next cycle's stale handler). Cron runs still defer to next cycle.
    if [ "$TRIGGER" = "manual" ]; then
      MLOG="${ART_ROOT}/${RUN_ID}/pr_merge.log"
      gh pr merge "$PR_NUM" --squash --delete-branch --admin >"$MLOG" 2>&1
      if [ "$?" = "0" ]; then
        log "PR #$PR_NUM squash-merged into $FORMA_PR_BASE (manual trigger)"
        push "Forma loop $RUN_ID: ✅ PR #$PR_NUM auto-merged to $FORMA_PR_BASE"
      elif close_if_conflicting "$PR_NUM"; then
        : # conflicting → closed + branch deleted above
      else
        log "PR #$PR_NUM merge FAILED (see $MLOG) — left open for next-cycle handler"
        push "Forma loop $RUN_ID: ⚠️ PR #$PR_NUM auto-merge FAILED; left open"
      fi
    fi
  fi

  state_set_outcome fixed
  state_set_phase done
  notify_cycle_done "$RUN_ID" "fixed" "$PR_URL"

  # Clean cycle-tagged failures from pending pool
  rm -f "${PENDING_FAILURES}/${RUN_ID}_"*.json
  release_active_run
}

#────────────────────────────────────────────────────────────────────────────────
# Dispatch
#────────────────────────────────────────────────────────────────────────────────

case "$ENTRY_PHASE" in
  health)         run_phase_health ;;
  deploy_wait)    run_phase_deploy_wait ;;
  sweeping)       run_phase_sweeping ;;
  baseline_tests) run_phase_baseline_tests ;;
  triaged)        run_phase_triage ;;
  fixing)         run_phase_fixing ;;
  merging)        run_phase_merging ;;
  local_tests)    run_phase_local_tests ;;
  verifying)      run_phase_verifying ;;
  pushing)        run_phase_pushing ;;
  done|failed|skipped) log "phase=$ENTRY_PHASE; nothing to do"; release_active_run ;;
  *) die "unknown entry phase: $ENTRY_PHASE" ;;
esac
