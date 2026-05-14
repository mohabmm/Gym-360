#!/usr/bin/env bash
# Refresh swagger + 5 role JWTs from the latest phase1 creds file.
# Owner JWT goes through /identity/select-gym to bake the tenant claim.
set -uo pipefail
API="${API:-http://localhost:8080}"
ROOT="$(cd "$(dirname "$0")"/.. && pwd)"
OUT="$ROOT/test_logs/preprod"
mkdir -p "$OUT"

curl -fsS "$API/swagger/v1/swagger.json" >/tmp/swagger.json
echo "  swagger bytes=$(wc -c </tmp/swagger.json)"

CREDS_FILE=$(ls -t "$ROOT"/test_logs/loop_*_creds.json 2>/dev/null | head -1)
if [[ -z "$CREDS_FILE" ]]; then
  echo "no creds file found, running phase1_bootstrap" >&2
  LOOP_N=$(date +%s | tail -c 6) bash "$ROOT/scripts/phase1_bootstrap.sh" >/tmp/phase1.log 2>&1 || { tail -40 /tmp/phase1.log; exit 1; }
  CREDS_FILE=$(ls -t "$ROOT"/test_logs/loop_*_creds.json | head -1)
fi
echo "  creds=$CREDS_FILE"

get() { python3 -c "import json,sys;d=json.load(open('$CREDS_FILE'));print(d['$1'].get('$2',''))"; }
GYM=$(python3 -c "import json;print(json.load(open('$CREDS_FILE'))['gymId'])")
echo "  gym=$GYM"

login() {
  local email="$1" pass="$2" tok
  for p in "$pass" "123456" "Owner123!"; do
    tok=$(curl -fsS -X POST "$API/api/v1/identity/login" -H 'Content-Type: application/json' \
      -d "{\"email\":\"$email\",\"password\":\"$p\"}" 2>/dev/null \
      | python3 -c "import json,sys
try:
    d=json.load(sys.stdin); print((d.get('data') or {}).get('accessToken',''))
except: print('')" )
    [[ -n "$tok" ]] && { echo "$tok"; return 0; }
  done
  return 1
}
select_gym() {
  local tok="$1" gym="$2"
  curl -fsS -X POST "$API/api/v1/identity/select-gym" \
    -H "Authorization: Bearer $tok" -H 'Content-Type: application/json' \
    -d "{\"gymId\":\"$gym\"}" \
    | python3 -c "import json,sys;d=json.load(sys.stdin);print((d.get('data') or d).get('accessToken',''))"
}

# Admin login also captures refresh token for the sweep
ADMIN_RESP=$(curl -fsS -X POST "$API/api/v1/identity/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$(get admin email)\",\"password\":\"$(get admin password)\"}" 2>/dev/null)
ADMIN=$(echo "$ADMIN_RESP" | python3 -c "import json,sys;d=json.load(sys.stdin);print((d.get('data') or {}).get('accessToken',''))")
echo "$ADMIN_RESP" | python3 -c "import json,sys;d=json.load(sys.stdin);print((d.get('data') or {}).get('refreshToken',''))" > "$OUT/refresh_admin.txt"
OWNER=$(  login "$(get owner email)"   "$(get owner password)" )
STAFF=$(  login "$(get staff email)"   "$(get staff password)" )
TRAINER=$(login "$(get trainer email)" "$(get trainer password)" )
TRAINEE=$(login "$(get trainee email)" "$(get trainee password)" )

# Owner needs select-gym to bake tenant claim into JWT
OWNER_SEL=$(select_gym "$OWNER" "$GYM" || true)
[[ -n "$OWNER_SEL" ]] && OWNER="$OWNER_SEL"

for v in ADMIN OWNER STAFF TRAINER TRAINEE; do
  [[ -z "${!v}" ]] && { echo "  ERROR: $v JWT empty"; exit 1; }
done

printf '%s' "$ADMIN"   > "$OUT/jwt_admin.txt"
printf '%s' "$OWNER"   > "$OUT/jwt_owner.txt"
printf '%s' "$STAFF"   > "$OUT/jwt_staff.txt"
printf '%s' "$TRAINER" > "$OUT/jwt_trainer.txt"
printf '%s' "$TRAINEE" > "$OUT/jwt_trainee.txt"
printf '%s' "$GYM"     > "$OUT/gym_id.txt"

for r in admin owner staff trainer trainee; do
  echo "  jwt_${r}: $(wc -c <"$OUT/jwt_${r}.txt" | tr -d ' ') bytes"
done
echo "  gym: $GYM"
