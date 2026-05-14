#!/usr/bin/env bash
# Phase 1 — bootstrap a full gym tenant via API.
# admin → create owner → register gym → owner → create staff + trainer + member.
# Writes test_logs/loop_${LOOP_N:-1}_creds.json for later phases.
set -euo pipefail

LOOP_N="${LOOP_N:-1}"
SUFFIX="l${LOOP_N}-$(date +%s | tail -c 7)"
API="${API:-http://localhost:8080/api/v1}"
OUT_DIR="$(cd "$(dirname "$0")"/.. && pwd)/test_logs"
OUT="$OUT_DIR/loop_${LOOP_N}_creds.json"
mkdir -p "$OUT_DIR"

ADMIN_EMAIL="admin@demo.local"
ADMIN_PASS="123456"
OWNER_EMAIL="owner-${SUFFIX}@gym.local"
OWNER_PASS="Owner123!"
STAFF_EMAIL="staff-${SUFFIX}@gym.local"
STAFF_PASS="Staff123!"
TRAINER_EMAIL="trainer-${SUFFIX}@gym.local"
TRAINER_PASS="123456"
TRAINEE_EMAIL="trainee-${SUFFIX}@gym.local"
TRAINEE_PASS="123456"
GYM_NAME="Loop Gym ${LOOP_N} ${SUFFIX}"

phone() { echo "+201$(( RANDOM * 99999 % 900000000 + 100000000 ))"; }
extract() { python3 -c "import json,sys; d=json.load(sys.stdin); k='$1'; print(d.get('data',{}).get(k, d.get(k,'')))"; }

echo ">> [1/8] Admin login"
ADMIN_TOKEN=$(curl -fsS -X POST "$API/identity/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" | extract accessToken)
[[ -n "$ADMIN_TOKEN" ]] || { echo "admin login failed"; exit 1; }

echo ">> [2/8] Create gym owner ($OWNER_EMAIL)"
OWNER_RESP=$(curl -fsS -X POST "$API/admin/owners" -H "Authorization: Bearer $ADMIN_TOKEN" -H 'Content-Type: application/json' -d "$(cat <<EOF
{"email":"$OWNER_EMAIL","password":"$OWNER_PASS","firstName":"Loop","lastName":"Owner${LOOP_N}","phone":"$(phone)","companyName":"Loop Fitness ${LOOP_N}","billingEmail":"billing-${SUFFIX}@gym.local","planTier":"Pro","dashboardMode":"Both"}
EOF
)")
OWNER_USER_ID=$(echo "$OWNER_RESP" | python3 -c "import json,sys;d=json.load(sys.stdin)['data'];print(d.get('userId',''))")
OWNER_AGG_ID=$(echo "$OWNER_RESP" | python3 -c "import json,sys;d=json.load(sys.stdin)['data'];print(d.get('ownerId',''))")
[[ -n "$OWNER_USER_ID" ]] || { echo "owner create failed: $OWNER_RESP"; exit 1; }
echo "   ownerUserId=$OWNER_USER_ID  ownerAggId=$OWNER_AGG_ID"

echo ">> [3/8] Register gym"
GYM_RESP=$(curl -fsS -X POST "$API/gyms" -H "Authorization: Bearer $ADMIN_TOKEN" -H 'Content-Type: application/json' -d "$(cat <<EOF
{"ownerId":"$OWNER_USER_ID","name":"$GYM_NAME","description":"Loop test gym","address":{"street":"1 Test St","city":"Cairo","state":"Cairo","country":"EG","zipCode":"11511"},"contact":{"email":"$OWNER_EMAIL","phone":"$(phone)"}}
EOF
)")
GYM_ID=$(echo "$GYM_RESP" | python3 -c "import json,sys;d=json.load(sys.stdin);print(d.get('data',d))")
echo "   gymId=$GYM_ID"

echo ">> [4/8] Owner login"
OWNER_TOKEN=$(curl -fsS -X POST "$API/identity/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$OWNER_EMAIL\",\"password\":\"$OWNER_PASS\"}" | extract accessToken)
[[ -n "$OWNER_TOKEN" ]] || { echo "owner login failed"; exit 1; }

# Resolve real gymId via /me or owner gym list (RegisterGym returned a Guid in body)
echo ">> [5/8] Resolve owner's gymId"
GYMS_LIST=$(curl -fsS "$API/owners/$OWNER_AGG_ID/gyms" -H "Authorization: Bearer $OWNER_TOKEN" 2>/dev/null || \
            curl -fsS "$API/gyms?ownerId=$OWNER_USER_ID" -H "Authorization: Bearer $OWNER_TOKEN")
GYM_ID=$(echo "$GYMS_LIST" | python3 -c "import json,sys
d=json.load(sys.stdin)
items=d.get('data',{}).get('items') if isinstance(d.get('data'),dict) else d.get('data',[])
if isinstance(items,list) and items:
    print(items[0].get('id',items[0].get('gymId','')))
")
[[ -n "$GYM_ID" ]] || { echo "could not resolve gymId from $GYMS_LIST"; exit 1; }
echo "   gymId=$GYM_ID"

# Owner must select-gym before gym-scoped writes (tenant claim baked into JWT)
echo ">> [5b/8] Owner select-gym"
SEL_RESP=$(curl -fsS -X POST "$API/identity/select-gym" -H "Authorization: Bearer $OWNER_TOKEN" -H 'Content-Type: application/json' -d "{\"gymId\":\"$GYM_ID\"}")
SEL_TOKEN=$(echo "$SEL_RESP" | extract accessToken)
[[ -n "$SEL_TOKEN" ]] && OWNER_TOKEN="$SEL_TOKEN"

echo ">> [6/8] Create staff (Receptionist) via admin endpoint"
# Note: owner-callable /gyms/{id}/staff requires existing UserId.
# /identity/users/staff-create hardcodes Trainee role.
# The only path that actually provisions a GymStaff user + assigns the gym staff
# role in one shot is /admin/gyms/{id}/staff/create — admin-scoped.
STAFF_RESP=$(curl -fsS -X POST "$API/admin/gyms/$GYM_ID/staff/create" -H "Authorization: Bearer $ADMIN_TOKEN" -H 'Content-Type: application/json' -d "$(cat <<EOF
{"email":"$STAFF_EMAIL","password":"$STAFF_PASS","firstName":"Loop","lastName":"Staff${LOOP_N}","phone":"$(phone)","staffSubRole":"Receptionist"}
EOF
)" || true)
STAFF_USER_ID=$(echo "$STAFF_RESP" | python3 -c "import json,sys
try:
    d=json.load(sys.stdin)
    print(d.get('data',{}).get('userId',d.get('userId','')))
except: print('')")
echo "   staffResp=$STAFF_RESP"
echo "   staffUserId=$STAFF_USER_ID"

echo ">> [7/8] Create trainer"
TRAINER_RESP=$(curl -fsS -X POST "$API/identity/users/staff-create-trainer" -H "Authorization: Bearer $OWNER_TOKEN" -H 'Content-Type: application/json' -d "$(cat <<EOF
{"email":"$TRAINER_EMAIL","password":"$TRAINER_PASS","firstName":"Loop","lastName":"Trainer${LOOP_N}","phone":"$(phone)","gender":"Male"}
EOF
)" || true)
TRAINER_USER_ID=$(echo "$TRAINER_RESP" | python3 -c "import json,sys
try:
    d=json.load(sys.stdin)
    print(d.get('data',{}).get('userId',d.get('userId','')))
except: print('')")
echo "   trainerResp=$TRAINER_RESP"
echo "   trainerUserId=$TRAINER_USER_ID"
if [[ -n "$TRAINER_USER_ID" ]]; then
  curl -fsS -X POST "$API/gyms/$GYM_ID/trainers" -H "Authorization: Bearer $OWNER_TOKEN" -H 'Content-Type: application/json' \
    -d "{\"trainerId\":\"$TRAINER_USER_ID\",\"bio\":\"Loop test trainer\",\"specializations\":[\"general\"]}" >/dev/null || echo "   (trainer link warn)"
  # auto-approve since we're owner
  curl -fsS -X POST "$API/gyms/$GYM_ID/trainers/$TRAINER_USER_ID/approve" -H "Authorization: Bearer $OWNER_TOKEN" >/dev/null || echo "   (trainer approve warn)"
fi

echo ">> [8/8] Create trainee (member)"
TRAINEE_RESP=$(curl -fsS -X POST "$API/identity/users/staff-create" -H "Authorization: Bearer $OWNER_TOKEN" -H 'Content-Type: application/json' -d "$(cat <<EOF
{"email":"$TRAINEE_EMAIL","password":"$TRAINEE_PASS","firstName":"Loop","lastName":"Trainee${LOOP_N}","phone":"$(phone)","gender":"Male","role":"Trainee"}
EOF
)" || true)
TRAINEE_USER_ID=$(echo "$TRAINEE_RESP" | python3 -c "import json,sys
try:
    d=json.load(sys.stdin)
    print(d.get('data',{}).get('userId',d.get('userId','')))
except: print('')")
echo "   traineeResp=$TRAINEE_RESP"
echo "   traineeUserId=$TRAINEE_USER_ID"
if [[ -n "$TRAINEE_USER_ID" ]]; then
  curl -fsS -X POST "$API/gyms/$GYM_ID/members" -H "Authorization: Bearer $OWNER_TOKEN" -H 'Content-Type: application/json' \
    -d "{\"userId\":\"$TRAINEE_USER_ID\",\"section\":\"Mens\"}" >/dev/null || echo "   (member link warn)"
fi

cat > "$OUT" <<EOF
{
  "loop": "$LOOP_N",
  "suffix": "$SUFFIX",
  "gymId": "$GYM_ID",
  "gymName": "$GYM_NAME",
  "admin":   { "email": "$ADMIN_EMAIL",   "password": "$ADMIN_PASS" },
  "owner":   { "email": "$OWNER_EMAIL",   "password": "$OWNER_PASS",   "userId": "$OWNER_USER_ID", "ownerId": "$OWNER_AGG_ID" },
  "staff":   { "email": "$STAFF_EMAIL",   "password": "$STAFF_PASS",   "userId": "$STAFF_USER_ID" },
  "trainer": { "email": "$TRAINER_EMAIL", "password": "$TRAINER_PASS", "userId": "$TRAINER_USER_ID" },
  "trainee": { "email": "$TRAINEE_EMAIL", "password": "$TRAINEE_PASS", "userId": "$TRAINEE_USER_ID" }
}
EOF
echo ">> creds → $OUT"
cat "$OUT"
