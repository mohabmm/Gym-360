#!/usr/bin/env bash
# Apply the durable-outbox hotfix (src/DB/hotfix_outbox_durable.sql) to the
# staging GymApp DB, executed INSIDE the gymapp-db container (SQL is loopback-only
# on the host, so we run sqlcmd from within the container).
#
# Run ON THE STAGING HOST from the repo root:
#   DB_SA_PASSWORD='<real sa password>' bash scripts/apply_outbox_hotfix.sh
#
# Idempotent: safe to re-run. Prints a coverage table at the end.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL_FILE="${HERE}/../src/DB/hotfix_outbox_durable.sql"
CONTAINER="${DB_CONTAINER:-gymapp-db}"
SA_PW="${DB_SA_PASSWORD:?set DB_SA_PASSWORD to the staging sa password}"

[ -f "$SQL_FILE" ] || { echo "missing $SQL_FILE"; exit 1; }
docker ps --format '{{.Names}}' | grep -qx "$CONTAINER" || { echo "container '$CONTAINER' not running"; exit 1; }

echo "→ copying hotfix into $CONTAINER"
docker cp "$SQL_FILE" "$CONTAINER:/tmp/hotfix_outbox_durable.sql"

# sqlcmd path differs across mssql image versions; try both.
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
docker exec "$CONTAINER" test -x "$SQLCMD" 2>/dev/null || SQLCMD="/opt/mssql-tools/bin/sqlcmd"

echo "→ executing hotfix (idempotent)"
docker exec -e SA_PW="$SA_PW" "$CONTAINER" \
  "$SQLCMD" -S localhost -U sa -P "$SA_PW" -C -b -i /tmp/hotfix_outbox_durable.sql

echo
echo "✓ outbox hotfix applied. Now verify the 500 is gone:"
echo "    scripts/role_action_sweep.py   # (from your laptop, browser UA)"
echo "  or just retry 'Add Member' in the staff web."
