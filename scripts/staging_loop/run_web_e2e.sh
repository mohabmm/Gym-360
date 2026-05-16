#!/usr/bin/env bash
# Playwright E2E against staging admin + staff webs.
# Args: <admin|staff>
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

APP="${1:?usage: $0 <admin|staff>}"
[ "$APP" = "admin" ] || [ "$APP" = "staff" ] || { echo "invalid app: $APP"; exit 1; }

if [ "${WEB_MODE:-local}" = "local" ]; then
  case "$APP" in
    admin) BASE_URL="$LOCAL_ADMIN_URL"; PROJECT="admin"; STATE_KEY="web_admin";;
    staff) BASE_URL="$LOCAL_STAFF_URL"; PROJECT="gym-staff"; STATE_KEY="web_staff";;
  esac
else
  case "$APP" in
    admin) BASE_URL="$STAGING_ADMIN"; PROJECT="admin"; STATE_KEY="web_admin";;
    staff) BASE_URL="$STAGING_STAFF"; PROJECT="gym-staff"; STATE_KEY="web_staff";;
  esac
fi
log "web_e2e $APP: BASE_URL=$BASE_URL (mode=${WEB_MODE:-local})"

ART_DIR="${ART_ROOT}/${RUN_ID}/web_${APP}"
mkdir -p "$ART_DIR"
state_set "sweeps.${STATE_KEY}.status" running

cd "${FORMA_ROOT}/gymapp-web"

LOG="$ART_DIR/playwright.log"
REPORT_DIR="$ART_DIR/report"
RESULT_JSON="$ART_DIR/results.json"

set +e
PLAYWRIGHT_HTML_REPORT="$REPORT_DIR" \
PLAYWRIGHT_JSON_OUTPUT_NAME="$RESULT_JSON" \
BASE_URL="$BASE_URL" \
STAGING_API="$STAGING_API" \
timeout "$TIMEOUT_WEB_E2E" npx playwright test \
  --project "$PROJECT" \
  --reporter=html,json \
  > "$LOG" 2>&1
RC=$?
set -e

FAIL_FILE="$ART_DIR/failures.json"
if [ -f "$RESULT_JSON" ]; then
  jq --arg app "$APP" '[.suites[]?.specs[]? | select(.tests[]?.results[]?.status != "passed")
      | {module: ("web-" + $app),
         source: "web_e2e",
         file: .file,
         title: .title,
         error: ([.tests[]?.results[]?.error?.message // empty] | join("\n"))}]' \
    "$RESULT_JSON" > "$FAIL_FILE" 2>/dev/null || echo "[]" > "$FAIL_FILE"
else
  echo "[{\"module\":\"web-${APP}\",\"source\":\"web_e2e\",\"error\":\"no results.json produced\",\"exit\":$RC}]" > "$FAIL_FILE"
fi

COUNT=$(jq 'length' "$FAIL_FILE")
state_set "sweeps.${STATE_KEY}.status" done
state_set "sweeps.${STATE_KEY}.exit_code" "$RC"
state_set "sweeps.${STATE_KEY}.failures" "$COUNT"
state_set "sweeps.${STATE_KEY}.artifact" "$REPORT_DIR"
log "web ${APP} done: $COUNT failures, exit=$RC"

[ "$COUNT" -gt 0 ] && cp "$FAIL_FILE" "${PENDING_FAILURES}/${RUN_ID}_web_${APP}.json"
exit 0
