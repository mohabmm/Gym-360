#!/usr/bin/env python3
"""Happy-path POSTs — exercise mutation handlers end-to-end with REAL bodies."""
import json, urllib.request, urllib.error
from pathlib import Path

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
JWT = lambda r: (ROOT/"test_logs/preprod"/f"jwt_{r}.txt").read_text().strip()
GYM = "bdaeb6ec-5302-45e3-aa75-e86756a22490"

def hit(method, path, role, body=None):
    url = BASE + path
    r = urllib.request.Request(url, method=method)
    r.add_header("Authorization", f"Bearer {JWT(role)}")
    r.add_header("X-Gym-Id", GYM)
    data = None
    if body is not None:
        r.add_header("Content-Type", "application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(r, data=data, timeout=20) as resp:
            return resp.status, resp.read().decode("utf-8","replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8","replace")
    except Exception as e:
        return 0, str(e)

results = []
def case(name, status, body, expect=(200,201,204), info=""):
    ok = status in expect
    results.append((ok, name, status, expect, body[:300]))
    if status >= 500 or status == 0:
        ok = False
    mark = "PASS" if ok else "FAIL"
    print(f"  {mark} [{status}] {name} {info[:60]}")
    if not ok:
        print(f"        body: {body[:200]}")
    return body

print("=== A. Workout creation (trainer) ===")
# Need to get an exercise ID first
s,b = hit("GET", "/api/v1/training/exercises?page=1&pageSize=5", "trainer")
ex_id = None
try:
    items = json.loads(b)["data"].get("items") or json.loads(b)["data"]
    if items: ex_id = items[0]["id"]
except: pass
print(f"  exercise to use: {ex_id}")

if ex_id:
    body = {"name":"Smoke Push Day","nameAr":"يوم دفع","description":"smoke","targetMuscleGroups":["Chest","Shoulders"],"difficulty":"Intermediate","estimatedDurationMinutes":45,"exercises":[{"exerciseId":ex_id,"orderIndex":0,"sets":3,"reps":"8-10","restSeconds":60,"notes":"warmup"}]}
    s,b = hit("POST","/api/v1/training/workouts","trainer",body=body)
    case("create workout", s, b)
else:
    print("  SKIP — no exercises")

print("\n=== B. Class creation (staff) ===")
TRAINER_ID = json.loads(hit("GET","/api/v1/identity/me","trainer")[1])["data"]["id"]
body = {"name":"Smoke Yoga","description":"smoke","trainerId":TRAINER_ID,"capacity":10,"waitlistEnabled":False,"schedule":{"dayOfWeek":1,"startTime":"08:00:00","durationMinutes":60,"isRecurring":True}}
s,b = hit("POST", f"/api/v1/gyms/{GYM}/classes", "staff", body=body)
case("create class", s, b)

print("\n=== C. Session package assign (staff → trainee) ===")
TRAINEE_ID = json.loads(hit("GET","/api/v1/identity/me","trainee")[1])["data"]["id"]
TRAINER_ID = json.loads(hit("GET","/api/v1/identity/me","trainer")[1])["data"]["id"]
STAFF_ID = json.loads(hit("GET","/api/v1/identity/me","staff")[1])["data"]["id"]
# Get a template
tmpl_resp = hit("GET","/api/v1/billing/package-templates?page=1&pageSize=5","staff")
TPL_ID = None
try:
    items = json.loads(tmpl_resp[1])["data"]
    if isinstance(items, list) and items: TPL_ID = items[0]["id"]
except: pass
body = {"gymId":GYM,"trainerId":TRAINER_ID,"memberId":TRAINEE_ID,"templateId":TPL_ID,"assignedBy":STAFF_ID,"expiresAt":"2026-12-31T00:00:00Z"}
s,b = hit("POST","/api/v1/billing/packages/assign","staff",body=body)
pkg_id = None
try: pkg_id = json.loads(b)["data"] if json.loads(b).get("data") else None
except: pass
case("assign session pkg", s, b, info=f"tmpl={TPL_ID} pkg={pkg_id}")

print("\n=== D. Quick-assign template (staff) ===")
# multiple of 4 between 4 and 40
body = {"gymId":GYM,"trainerId":TRAINER_ID,"memberId":TRAINEE_ID,"assignedBy":STAFF_ID,"sessionCount":8,"pricePerSession":300.0,"currency":"EGP","expiresAt":"2026-12-31T00:00:00Z"}
s,b = hit("POST","/api/v1/billing/packages/quick-assign","staff",body=body)
case("quick-assign pkg", s, b)

print("\n=== E. Record payment (staff) ===")
body = {"gymId":GYM,"memberId":TRAINEE_ID,"amount":500.0,"currency":"EGP","paymentMethod":"Cash","purpose":"Membership","paymentDate":"2026-05-10","description":"smoke","referenceNumber":"SMK-001","recordedBy":STAFF_ID}
s,b = hit("POST","/api/v1/billing/payments","staff",body=body)
case("record payment", s, b)

print("\n=== F. Body metric log (trainee) ===")
body = {"date":"2026-05-10","weightKg":75.5,"bodyFatPercent":18.0,"chest":100.0,"waist":85.0,"hips":95.0,"arms":35.0,"thighs":60.0,"notes":"smoke"}
s,b = hit("POST","/api/v1/progress/body-metrics","trainee",body=body)
case("log body metric", s, b)

print("\n=== G. Food log (trainee) ===")
body = {"gymId":GYM,"date":"2026-05-10","mealType":"Breakfast","foodItems":[{"name":"Oats","quantity":50,"unit":"g","calories":190,"protein":7,"carbs":33,"fat":3}]}
s,b = hit("POST","/api/v1/nutrition/food-log","trainee",body=body)
case("log food", s, b)

print("\n=== H. Send chat message (trainee) ===")
body = {"gymId":GYM,"participant2":TRAINER_ID}
s,b = hit("POST","/api/v1/communication/conversations","trainee",body=body)
case("create conversation", s, b)

print("\n=== I. Submit body weight target (trainee) ===")
body = {"targetWeightKg":72.0,"targetDate":"2026-08-01"}
s,b = hit("PUT","/api/v1/progress/body-weight-goal","trainee",body=body)
case("set body weight goal", s, b, expect=(200,201,204,400,404))

print("\n=== J. Trainer submits availability (trainer) ===")
body = {"trainerId":TRAINER_ID,"gymId":GYM,"sessionDurationMinutes":60,"weeklySlots":[{"dayOfWeek":1,"startTime":"09:00:00","endTime":"17:00:00"},{"dayOfWeek":3,"startTime":"09:00:00","endTime":"17:00:00"}]}
s,b = hit("PUT","/api/v1/scheduling/availability","trainer",body=body)
case("set availability", s, b)

print("\n=== K. Add device token (trainee) ===")
body = {"token":"smoke-fcm-token-123","platform":"Ios","appVersion":"1.0.0"}
s,b = hit("POST","/api/v1/notifications/device-tokens","trainee",body=body)
case("register device token", s, b)

ok = sum(1 for r in results if r[0])
print(f"\n=== Total: {ok}/{len(results)} happy-path POSTs ===")
crashes = [r for r in results if r[2] >= 500 or r[2] == 0]
if crashes:
    print("\n=== 5xx / network crashes (BAD) ===")
    for r in crashes:
        print(f"  [{r[2]}] {r[1]}")
        print(f"      {r[4]}")
else:
    print("Zero 5xx in happy-path POSTs.")

(ROOT/"test_logs/preprod/happypath_results.json").write_text(json.dumps([{"ok":r[0],"name":r[1],"status":r[2],"expected":list(r[3]),"body":r[4][:300]} for r in results], indent=2))
