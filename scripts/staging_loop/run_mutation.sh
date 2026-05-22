#!/usr/bin/env bash
# Daily Stryker.NET mutation testing. Surviving mutants → _pending_failures for next cycle.
# Has its own state file under artifacts/mutation/$RUN_ID.
# Shares /tmp/forma_staging_loop.lock w/ main loop (waits up to 1h, else skip).
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

RUN_ID="mut-$(date -u +%Y%m%d)"
DIR="${ART_ROOT}/mutation/${RUN_ID}"
mkdir -p "$DIR"
STATE="$DIR/state.json"

# Acquire lock or wait up to 1h (poll 5min)
exec 200>"$LOCK_FILE"
WAITED=0
while ! flock -n 200; do
  if [ "$WAITED" -ge 3600 ]; then
    log "mutation: lock held >1h, skipping today's run"
    exit 0
  fi
  log "mutation: lock busy, waiting 300s"
  sleep 300
  WAITED=$((WAITED+300))
done
echo $$ > "$PID_FILE"
trap 'rm -f "$PID_FILE"; flock -u 200 2>/dev/null || true' EXIT

cd "$FORMA_ROOT"

# Init state
cat > "$STATE" <<EOF
{
  "run_id": "${RUN_ID}",
  "phase": "running",
  "started_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "projects": {
    "Domain":      {"status":"pending"},
    "Application": {"status":"pending"},
    "Functional":  {"status":"pending"}
  }
}
EOF

# Install stryker tool if absent
if ! dotnet tool list -g | grep -q dotnet-stryker; then
  log "mutation: installing dotnet-stryker"
  dotnet tool install -g dotnet-stryker 2>&1 | tee "$DIR/install.log"
fi
export PATH="$PATH:$HOME/.dotnet/tools"

# bash 3.2 (macOS /bin/bash) has no associative arrays — use a case lookup.
test_proj() {
  case "$1" in
    Domain)      echo "tests/GymApp.Tests.Domain/GymApp.Tests.Domain.csproj";;
    Application) echo "tests/GymApp.Tests.Application/GymApp.Tests.Application.csproj";;
    Functional)  echo "tests/GymApp.Tests.Functional/GymApp.Tests.Functional.csproj";;
    *) echo "";;
  esac
}

# Update state helper local
ms_set() {
  local path="$1"; local v="$2"
  local tmp; tmp="$(mktemp "${STATE}.XXXXXX")"
  if echo "$v" | jq -e '.' >/dev/null 2>&1; then
    jq --argjson v "$v" ".${path} = \$v" "$STATE" > "$tmp"
  else
    jq --arg v "$v" ".${path} = \$v" "$STATE" > "$tmp"
  fi
  mv "$tmp" "$STATE"
}

OVERALL_BREAK=0
for kind in Domain Application Functional; do
  TPROJ="$(test_proj "$kind")"
  if [ ! -f "$TPROJ" ]; then ms_set "projects.${kind}.status" missing; continue; fi
  ms_set "projects.${kind}.status" running
  OUT="$DIR/$kind"
  mkdir -p "$OUT"
  set +e
  timeout "$TIMEOUT_MUTATION" dotnet stryker \
    --test-project "$TPROJ" \
    --reporter json --reporter html \
    --output "$OUT" \
    --break-at "$MUTATION_THRESHOLD_BREAK" \
    > "$OUT/stryker.log" 2>&1
  RC=$?
  set -e
  ms_set "projects.${kind}.status" done
  ms_set "projects.${kind}.exit_code" "$RC"
  # Stryker writes mutation-report.json
  REPORT="$(find "$OUT" -name 'mutation-report.json' | head -1)"
  if [ -n "$REPORT" ]; then
    SCORE=$(jq '.thresholds.high as $h | (.files | to_entries | map(.value.mutants[].status) | flatten | group_by(.) | map({(.[0]): length}) | add) as $s
      | ((($s.Killed // 0) + ($s.Timeout // 0)) * 100 / (((($s.Killed // 0)+($s.Survived // 0)+($s.NoCoverage // 0)+($s.Timeout // 0))) | if . == 0 then 1 else . end))' "$REPORT")
    ms_set "projects.${kind}.score" "$SCORE"
    SURV=$(jq '[.files | to_entries[] | .value.mutants[] | select(.status=="Survived")] | length' "$REPORT")
    ms_set "projects.${kind}.survived" "$SURV"
  fi
  if [ "$RC" != "0" ]; then OVERALL_BREAK=1; fi
done

# Convert surviving mutants → pending failures for next staging cycle
"$(dirname "$0")/mutants_to_failures.sh" "$DIR" || true

ms_set phase done
ms_set ended_at "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
log "mutation cycle done; break=$OVERALL_BREAK"

# If broken threshold, write marker that staging loop pre-flight checks
if [ "$OVERALL_BREAK" = "1" ]; then
  echo "$RUN_ID" > "${ART_ROOT}/mutation/_break_marker"
  log "mutation: break threshold hit; staging loop will gate next PR"
fi

exit 0
