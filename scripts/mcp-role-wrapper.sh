#!/usr/bin/env bash
# Fetches a fresh JWT for the given demo-user role and launches swagger-mcp with it.
# Usage: mcp-role-wrapper.sh <admin|owner|staff|trainer|trainee>
# Token lifetime ~1h — restart Claude Code (or /mcp reconnect) when stale.
set -euo pipefail

ROLE="${1:?role required: admin|owner|staff|trainer|trainee}"
API_BASE="${API_BASE_URL:-http://localhost:8080}"
PASSWORD="${DEMO_PASSWORD:-123456}"

TOKEN=$(curl -sf -X POST "$API_BASE/api/v1/identity/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${ROLE}@demo.local\",\"password\":\"${PASSWORD}\"}" \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['accessToken'])")

if [[ -z "$TOKEN" ]]; then
  echo "mcp-role-wrapper: login failed for ${ROLE}@demo.local" >&2
  exit 1
fi

exec env \
  API_BASE_URL="$API_BASE" \
  API_DOCS_URL="$API_BASE/swagger/v1/swagger.json" \
  API_KEY="$TOKEN" \
  npx -y swagger-mcp@latest
