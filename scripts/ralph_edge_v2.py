#!/usr/bin/env python3
"""Iter 4 — extended edge cases: pagination, unicode, time zones, idempotency, self-targeting."""
import json, urllib.request, urllib.error, urllib.parse
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"
GYM = (OUT / "gym_id.txt").read_text().strip()
JWTS = {r: (OUT / f"jwt_{r}.txt").read_text().strip() for r in ["admin","owner","staff","trainer","trainee"]}

def hit(method, path, role=None, token=None, body=None, headers=None, query=None):
    url = BASE + path
    if query: url += "?" + urllib.parse.urlencode(query)
    req = urllib.request.Request(url, method=method.upper())
    tok = token if token is not None else (JWTS.get(role) if role else None)
    if tok: req.add_header("Authorization", f"Bearer {tok}")
    req.add_header("X-Gym-Id", GYM)
    for k,v in (headers or {}).items(): req.add_header(k, v)
    data = None
    if body is not None and method.lower() in ("post","put","patch"):
        req.add_header("Content-Type","application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data=data, timeout=30) as r:
            return r.status, r.read().decode("utf-8","replace")[:300]
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8","replace")[:300]
    except Exception as e:
        return 0, f"{type(e).__name__}: {e}"

cases = []
def check(name, expected, status, snippet=""):
    ok = status in expected
    cases.append({"name":name,"expected":expected,"got":status,"ok":ok,"snippet":snippet[:120]})

# --- PAGINATION boundaries
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner", query={"page":0,"pageSize":10})
check("page=0", [200,400], s, b)

s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner", query={"page":1,"pageSize":0})
check("pageSize=0", [200,400], s, b)

s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner", query={"page":1,"pageSize":-5})
check("pageSize=-5", [200,400], s, b)

s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner", query={"page":1,"pageSize":10000})
check("pageSize=huge", [200,400], s, b)

s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner", query={"page":999,"pageSize":5})
check("page=999-empty", [200], s, b)

# --- Unicode / Arabic / emoji in body
s,b = hit("POST", f"/api/v1/training/exercises", role="trainer",
         body={"name":"كرة الكاحل 🦵","category":"general","muscleGroups":["legs"],
               "equipment":"none","description":"اختبار يونيكود"})
check("unicode-arabic-emoji", [201,400,409], s, b)

# --- Time zone: very far past
s,b = hit("PUT", "/api/v1/nutrition/water-log/target", role="trainee",
         body={"gymId":GYM,"date":"1900-01-01","targetMl":2000})
check("date-1900", [204,400], s, b)

# --- Negative timezone offset (peak hours)
s,b = hit("GET", f"/api/v1/gyms/{GYM}/analytics/peak-hours", role="owner",
         query={"tzOffsetMinutes":-720})
check("peak-tz-neg720", [200,400], s, b)

# --- Self-targeting cross resource
trainee_id = "f89ca860-23b9-4f01-a8ec-7bc4aaccff87"
trainer_id = "5452d4d9-3ef2-4e0f-ae57-bf19956a824b"
# trainer rating: trainer rating themselves
s,b = hit("POST", "/api/v1/gamification/trainer-ratings", role="trainer",
         body={"gymId":GYM,"trainerId":trainer_id,"rating":5,"review":"self"})
check("self-rate-trainer", [400,403,201,409], s, b)

# --- Idempotency: rapid duplicate POST should yield 1 success, others 4xx (not 5xx)
def dup_water(_):
    return hit("POST","/api/v1/nutrition/water-log", role="trainee",
              body={"gymId":GYM,"amountMl":200,"loggedAt":"2026-05-12T13:00:00Z"})
with ThreadPoolExecutor(max_workers=10) as ex:
    r = list(ex.map(dup_water, range(10)))
codes = [x[0] for x in r]
fives = sum(1 for c in codes if c >= 500)
check("dup-water-10x-no-5xx", [0], fives, f"codes={sorted(set(codes))}")

# --- Pagination + invalid sort
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner",
         query={"page":1,"pageSize":5,"sort":"invalid_column DESC"})
check("invalid-sort", [200,400], s, b)

# --- Bogus enum
s,b = hit("POST", f"/api/v1/admin/owners", role="admin",
         body={"email":f"x{__import__('time').time_ns()}@x.com","password":"123456","firstName":"a",
               "lastName":"b","phone":"+200","companyName":"c","billingEmail":"x@x",
               "planTier":"INVALID_PLAN","dashboardMode":"Both"})
check("bogus-enum-planTier", [400], s, b)

# --- Empty list body
s,b = hit("POST", f"/api/v1/training/workouts", role="trainer",
         body={"trainerId":trainer_id,"gymId":GYM,"name":"empty",
               "description":"x","estimatedDurationMinutes":30,
               "targetMuscleGroups":[],"isTemplate":True,"exercises":[]})
check("empty-arrays", [201,400], s, b)

# --- Future date in past field
s,b = hit("POST","/api/v1/nutrition/water-log", role="trainee",
         body={"gymId":GYM,"amountMl":200,"loggedAt":"2099-01-01T12:00:00Z"})
check("future-loggedAt", [201,400], s, b)

# --- Excessive query depth
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner",
         query={"page":1,"pageSize":5,"search":"A"*2000})
check("huge-search-string", [200,400,414], s, b)

# --- Wrong role on /me/ endpoints (trainee accesses /me/ — should work)
s,b = hit("GET","/api/v1/identity/me", role="trainee")
check("trainee-self-me", [200], s, b)

# --- Owner accessing /identity/me (own profile)
s,b = hit("GET","/api/v1/identity/me", role="owner")
check("owner-self-me", [200], s, b)

# --- Wrong gym in body vs header (cross-tenant attack)
s,b = hit("POST","/api/v1/nutrition/water-log", role="trainee",
         body={"gymId":"00000000-0000-0000-0000-000000000999","amountMl":200,"loggedAt":"2026-05-12T13:00:00Z"})
check("body-gym-mismatch", [400,403], s, b)

# --- Refresh token replay: mint a fresh refresh token first (others may have revoked the cached one)
def _mint_refresh():
    req = urllib.request.Request(BASE + "/api/v1/identity/login", method="POST")
    req.add_header("Content-Type","application/json")
    try:
        with urllib.request.urlopen(req, data=json.dumps({"email":"admin@demo.local","password":"123456"}).encode(), timeout=10) as r:
            return ((json.loads(r.read().decode()).get("data") or {}).get("refreshToken")) or ""
    except Exception: return ""
fresh_rt = _mint_refresh()
if fresh_rt:
    rt = fresh_rt
    s1,b1 = hit("POST","/api/v1/identity/refresh-token", body={"refreshToken":rt})
    s2,b2 = hit("POST","/api/v1/identity/refresh-token", body={"refreshToken":rt})
    # First should 200; second either 200 (non-rotating) or 401 (rotating)
    check("refresh-replay-1", [200], s1, b1)
    check("refresh-replay-2", [200,401], s2, b2)
else:
    check("refresh-mint", [200], 0, "could not mint fresh refresh token")

# Report
print(f"{'NAME':35} {'EXPECT':24} {'GOT':6} {'OK':4}")
bugs=[]
for c in cases:
    mk = "✓" if c["ok"] else "✗"
    print(f"  {c['name']:33} {str(c['expected']):24} {str(c['got']):6} {mk}  {c['snippet']}")
    if not c["ok"]: bugs.append(c)
print(f"\n=== {len(cases)-len(bugs)}/{len(cases)} pass ===")
if bugs:
    for b in bugs: print(f"  ✗ {b['name']}: expected {b['expected']}, got {b['got']} — {b['snippet']}")
(OUT/"edge_v2_results.json").write_text(json.dumps(cases, indent=2))
