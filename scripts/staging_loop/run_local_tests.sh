#!/usr/bin/env bash
# Run all local tests. Arg: <baseline|gate>
#  baseline = pre-fix snapshot, failures fed to triage
#  gate     = post-fix run, must be all green to push
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

KIND="${1:-gate}"
case "$KIND" in
  baseline) STATE_KEY="baseline_tests";;
  gate)     STATE_KEY="local_tests";;
  *) echo "usage: $0 <baseline|gate>"; exit 1;;
esac

ART_DIR="${ART_ROOT}/${RUN_ID}/${KIND}_tests"
mkdir -p "$ART_DIR"
state_set "${STATE_KEY}.status" running
state_set "${STATE_KEY}.started_at" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"

cd "$FORMA_ROOT"

# Fail fast if the Docker daemon is dead/unresponsive. Without this every
# `docker compose` call below blocks forever (observed: 8h wedge on a dead
# Docker Desktop). `timeout` is the gtimeout shim on PATH (config.sh).
if ! timeout 20 docker info >/dev/null 2>&1; then
  log "${KIND}: Docker daemon unreachable — gate cannot run, marking failed"
  state_set "${STATE_KEY}.status" failed
  state_set "${STATE_KEY}.reason" "docker_daemon_down"
  echo '[{"source":"local_tests","module":"infra","error":"docker daemon unreachable"}]' > "$ART_DIR/failures.json"
  exit 1
fi

# Ensure SQL + Redis up (every docker call timeout-wrapped so a hung daemon
# fails the gate fast instead of blocking the whole loop).
if ! timeout 30 docker compose -f docker-compose.dev.yml ps --status running --services 2>/dev/null | grep -q sqlserver; then
  log "${KIND}: starting sqlserver+redis containers"
  timeout 180 docker compose -f docker-compose.dev.yml up -d sqlserver redis 2>&1 | tee -a "$ART_DIR/docker.log" || true
  # wait for SQL ready (up to 60s)
  for i in $(seq 1 30); do
    if timeout 15 docker compose -f docker-compose.dev.yml exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Test_P@ss2024!" -Q "SELECT 1" -C >/dev/null 2>&1; then
      break
    fi
    sleep 2
  done
fi

FAIL_FILE="$ART_DIR/failures.json"
echo "[]" > "$FAIL_FILE"

# bash 3.2 (macOS /bin/bash) has no associative arrays — use a case lookup.
suite_proj() {
  case "$1" in
    architecture) echo "tests/GymApp.Tests.Architecture/GymApp.Tests.Architecture.csproj";;
    domain)       echo "tests/GymApp.Tests.Domain/GymApp.Tests.Domain.csproj";;
    application)  echo "tests/GymApp.Tests.Application/GymApp.Tests.Application.csproj";;
    integration)  echo "tests/GymApp.Tests.Integration/GymApp.Tests.Integration.csproj";;
    functional)   echo "tests/GymApp.Tests.Functional/GymApp.Tests.Functional.csproj";;
    *) echo "";;
  esac
}

OVERALL=0
for suite in architecture domain application integration functional; do
  proj="$(suite_proj "$suite")"
  if [ ! -f "$proj" ]; then
    state_set "${STATE_KEY}.suites.${suite}.status" missing
    continue
  fi
  log "${KIND}: running $suite"
  state_set "${STATE_KEY}.suites.${suite}.status" running
  LOG="$ART_DIR/${suite}.log"
  TRX="$ART_DIR/${suite}.trx"
  set +e
  timeout "$TIMEOUT_LOCAL_TESTS" dotnet test "$proj" \
    -c Release \
    --logger "trx;LogFileName=${suite}.trx" \
    --results-directory "$ART_DIR" \
    > "$LOG" 2>&1
  RC=$?
  set -e
  PASS=$(grep -oE 'Passed:.*[0-9]+' "$LOG" | tail -1 | awk '{print $2}' || echo 0)
  FAIL=$(grep -oE 'Failed:.*[0-9]+' "$LOG" | tail -1 | awk '{print $2}' || echo 0)
  state_set "${STATE_KEY}.suites.${suite}.status" done
  state_set "${STATE_KEY}.suites.${suite}.passed" "${PASS:-0}"
  state_set "${STATE_KEY}.suites.${suite}.failed" "${FAIL:-0}"
  state_set "${STATE_KEY}.suites.${suite}.exit_code" "$RC"
  if [ "$RC" != "0" ]; then
    OVERALL=1
    # Parse trx for failed test names
    if [ -f "$TRX" ]; then
      python3 - <<PY >> "$FAIL_FILE.tmp"
import json, xml.etree.ElementTree as ET
ns = {"x":"http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
try:
    t = ET.parse("$TRX").getroot()
    for r in t.findall(".//x:UnitTestResult[@outcome='Failed']", ns):
        name = r.get("testName") or "?"
        err = r.find("x:Output/x:ErrorInfo/x:Message", ns)
        st  = r.find("x:Output/x:ErrorInfo/x:StackTrace", ns)
        print(json.dumps({
            "module": "$suite",
            "source": "${KIND}_tests",
            "test": name,
            "error": (err.text or "")[:1500] if err is not None else "",
            "stack": (st.text or "")[:1500] if st is not None else ""
        }))
except Exception as e:
    print(json.dumps({"module":"$suite","source":"${KIND}_tests","error":f"trx parse: {e}"}))
PY
    fi
  fi
done

# Frontend tests (optional — skip if no script defined)
if [ -f "${FORMA_ROOT}/gym-360-web/package.json" ]; then
  if jq -e '.scripts.test' "${FORMA_ROOT}/gym-360-web/package.json" >/dev/null 2>&1; then
    log "${KIND}: web unit tests"
    LOG="$ART_DIR/web_unit.log"
    set +e
    (cd "${FORMA_ROOT}/gym-360-web" && timeout 600 pnpm -r --filter '!*e2e*' test) > "$LOG" 2>&1
    WRC=$?
    set -e
    state_set "${STATE_KEY}.suites.web_unit.status" done
    state_set "${STATE_KEY}.suites.web_unit.exit_code" "$WRC"
    [ "$WRC" != "0" ] && OVERALL=1 && \
      echo "{\"module\":\"web-unit\",\"source\":\"${KIND}_tests\",\"error\":\"web unit suite failed; see web_unit.log\"}" >> "$FAIL_FILE.tmp"
  fi
fi

# Flutter tests
if [ -f "${FORMA_ROOT}/gymapp-mobile/melos.yaml" ]; then
  log "${KIND}: flutter tests"
  LOG="$ART_DIR/flutter.log"
  set +e
  (cd "${FORMA_ROOT}/gymapp-mobile" && timeout 900 melos run test) > "$LOG" 2>&1
  FRC=$?
  set -e
  state_set "${STATE_KEY}.suites.flutter.status" done
  state_set "${STATE_KEY}.suites.flutter.exit_code" "$FRC"
  [ "$FRC" != "0" ] && OVERALL=1 && \
    echo "{\"module\":\"flutter\",\"source\":\"${KIND}_tests\",\"error\":\"flutter melos test failed; see flutter.log\"}" >> "$FAIL_FILE.tmp"
fi

# Convert ndjson failures → JSON array
if [ -f "$FAIL_FILE.tmp" ]; then
  jq -s '.' "$FAIL_FILE.tmp" > "$FAIL_FILE" || echo "[]" > "$FAIL_FILE"
  rm -f "$FAIL_FILE.tmp"
fi

COUNT=$(jq 'length' "$FAIL_FILE")
state_set "${STATE_KEY}.status" done
state_set "${STATE_KEY}.failures" "$COUNT"
state_set "${STATE_KEY}.overall_exit" "$OVERALL"
log "${KIND}_tests done: $COUNT failures, overall_exit=$OVERALL"

# Baseline failures join triage pool; gate failures block PR (caller handles)
if [ "$KIND" = "baseline" ] && [ "$COUNT" -gt 0 ]; then
  cp "$FAIL_FILE" "${PENDING_FAILURES}/${RUN_ID}_baseline_tests.json"
fi

exit "$OVERALL"
