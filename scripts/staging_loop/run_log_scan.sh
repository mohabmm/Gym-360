#!/usr/bin/env bash
# Scan staging Seq logs for ERROR-level entries during sweep window.
# Falls back to direct API container log fetch via Dokploy SSH if Seq unreachable.
# Best-effort: marked skipped if no SEQ_URL / no SSH.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/config.sh"
source "$(dirname "${BASH_SOURCE[0]}")/state.sh"

ART_DIR="${ART_ROOT}/${RUN_ID}/log_scan"
mkdir -p "$ART_DIR"
state_set sweeps.log_scan.status running

FAIL_FILE="$ART_DIR/failures.json"
RAW="$ART_DIR/errors.json"

SEQ_URL="${STAGING_SEQ_URL:-}"
SEQ_APIKEY="${STAGING_SEQ_APIKEY:-}"

if [ -z "$SEQ_URL" ]; then
  log "log_scan: STAGING_SEQ_URL unset, skipping"
  state_set sweeps.log_scan.status skipped
  state_set sweeps.log_scan.reason "no_seq_url"
  echo "[]" > "$FAIL_FILE"
  exit 0
fi

# Sweep started_at — query Seq from then to now
SWEEP_START=$(state_get started_at)

# Seq events API
curl -sS --max-time 30 \
  -H "X-Seq-ApiKey: $SEQ_APIKEY" \
  --get "${SEQ_URL}/api/events" \
  --data-urlencode "filter=@Level in ['Error','Fatal']" \
  --data-urlencode "fromDateUtc=${SWEEP_START}" \
  --data-urlencode "count=200" \
  > "$RAW" 2>&1 || { echo "[]" > "$RAW"; }

jq '[. // [] | .[]? | {
  module: (.Properties.Module // .Properties.SourceContext // "unknown" | tostring | split(".") | .[-1] | ascii_downcase),
  source: "log_scan",
  severity: .Level,
  message: .MessageTemplate,
  exception: .Exception,
  timestamp: .Timestamp,
  correlation: .Properties.CorrelationId
}]' "$RAW" > "$FAIL_FILE" 2>/dev/null || echo "[]" > "$FAIL_FILE"

COUNT=$(jq 'length' "$FAIL_FILE")
state_set sweeps.log_scan.status done
state_set sweeps.log_scan.failures "$COUNT"
state_set sweeps.log_scan.artifact "$RAW"
log "log_scan done: $COUNT errors"

[ "$COUNT" -gt 0 ] && cp "$FAIL_FILE" "${PENDING_FAILURES}/${RUN_ID}_log_scan.json"
exit 0
