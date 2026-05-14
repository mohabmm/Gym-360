#!/usr/bin/env python3
"""Iter 5 — CRUD lifecycle, FK violations, replay attacks, write-after-delete."""
import json, urllib.request, urllib.error, urllib.parse, time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"
GYM = (OUT / "gym_id.txt").read_text().strip()
JWTS = {r: (OUT / f"jwt_{r}.txt").read_text().strip() for r in ["admin","owner","staff","trainer","trainee"]}

def hit(method, path, role=None, token=None, body=None, query=None):
    url = BASE + path
    if query: url += "?" + urllib.parse.urlencode(query)
    req = urllib.request.Request(url, method=method.upper())
    tok = token if token is not None else (JWTS.get(role) if role else None)
    if tok: req.add_header("Authorization", f"Bearer {tok}")
    req.add_header("X-Gym-Id", GYM)
    data = None
    if body is not None and method.lower() in ("post","put","patch"):
        req.add_header("Content-Type","application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data=data, timeout=30) as r:
            return r.status, r.read().decode("utf-8","replace")[:400]
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8","replace")[:400]
    except Exception as e:
        return 0, f"{type(e).__name__}: {e}"

cases = []
def check(name, expected, status, snip=""):
    cases.append({"name":name,"expected":expected,"got":status,"ok":status in expected,"snippet":snip[:120]})

# === FK violations ===
fake_id = "00000000-0000-0000-0000-000000000999"
# Book session with non-existent trainer
s,b = hit("POST", "/api/v1/scheduling/sessions", role="trainee",
         body={"gymId":GYM,"trainerId":fake_id,"sessionType":"PT","durationMinutes":60,
               "startTime":"2026-05-13T10:00:00Z","notes":"fk-test"})
check("book-fake-trainer", [400,404], s, b)

# Rate non-existent trainer
s,b = hit("POST","/api/v1/gamification/trainer-ratings", role="trainee",
         body={"gymId":GYM,"trainerId":fake_id,"sessionId":fake_id,"rating":5,"review":"x"})
check("rate-fake-trainer", [400,404], s, b)

# Body metric for non-existent client
s,b = hit("POST","/api/v1/progress/body-metrics/client", role="trainer",
         body={"gymId":GYM,"clientId":fake_id,"weightKg":80,"recordedAt":"2026-05-12T12:00:00Z"})
check("body-metric-fake-client", [400,403,404], s, b)

# Foreign session start
s,b = hit("POST", f"/api/v1/scheduling/sessions/{fake_id}/start", role="trainer")
check("start-fake-session", [400,404], s, b)

# === REPLAY/IDEMPOTENCY ===
# Same booking twice (same trainer, same slot) — second should 409 not 500
import datetime
ts = (datetime.datetime.utcnow() + datetime.timedelta(days=2)).replace(microsecond=0).isoformat() + "Z"
trainer_id = "5452d4d9-3ef2-4e0f-ae57-bf19956a824b"
body = {"gymId":GYM,"trainerId":trainer_id,"sessionType":"PT","durationMinutes":60,
        "startTime":ts,"notes":"replay"}
s1,b1 = hit("POST","/api/v1/scheduling/sessions", role="trainee", body=body)
s2,b2 = hit("POST","/api/v1/scheduling/sessions", role="trainee", body=body)
check("book-first", [200,201,400,403], s1, b1)
check("book-replay", [200,201,400,403,409], s2, b2)
# Critical: no 5xx
check("book-replay-no-5xx", [0], 1 if max(s1,s2) >= 500 else 0, f"first={s1} replay={s2}")

# === DELETE-then-READ ===
# Try to read a member after delete (note: DELETE is destructive, skip actual delete; test 404 path)
# Use a fake member id to simulate stale read
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members/{fake_id}", role="staff")
check("read-fake-member", [404], s, b)
s,b = hit("PUT", f"/api/v1/gyms/{GYM}/members/{fake_id}", role="staff",
         body={"firstName":"x","lastName":"y","phoneNumber":"+201234567890"})
check("update-fake-member", [400,404], s, b)

# === HTTP method abuse ===
# DELETE the trainee's own /me account (would actually delete — destructive, skip)
# But GET on a POST-only route:
s,b = hit("DELETE", f"/api/v1/gyms/{GYM}/check-in", role="trainee")
check("delete-checkin-bad-method", [405,404], s, b)

# PATCH on PUT-only
s,b = hit("PATCH", "/api/v1/nutrition/water-log/target", role="trainee", body={"targetMl":2500})
check("patch-on-put", [405,404], s, b)

# === LARGE PAYLOADS ===
# 500KB body
big_notes = "x" * 500_000
s,b = hit("POST", "/api/v1/scheduling/sessions", role="trainee",
         body={"gymId":GYM,"trainerId":trainer_id,"sessionType":"PT","durationMinutes":60,
               "startTime":"2026-05-15T10:00:00Z","notes":big_notes})
check("500kb-body", [201,400,413], s, b[:80])

# === CASE SENSITIVITY ===
# Status enum: lowercase
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner",
         query={"page":1,"pageSize":5,"status":"ACTIVE"})
check("status-uppercase", [200,400], s, b)
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner",
         query={"page":1,"pageSize":5,"status":"active"})
check("status-lowercase", [200,400], s, b)

# === EMPTY GUID ===
s,b = hit("GET", f"/api/v1/gyms/00000000-0000-0000-0000-000000000000/members", role="owner",
         query={"page":1,"pageSize":5})
check("empty-guid-gym", [400,403,404], s, b)

# === MEMBER WORKFLOW ===
# Add member (owner) then re-add (dup email)
em = f"sweep-{int(time.time())}@test.local"
add_body = {"email":em,"password":"123456","firstName":"sw","lastName":"ep","phone":"+201111111111","section":"Mens","gender":"Male","role":"Trainee"}
s,b = hit("POST", "/api/v1/identity/users/staff-create", role="owner", body=add_body)
check("create-member-first", [200,201,400], s, b)
s,b = hit("POST", "/api/v1/identity/users/staff-create", role="owner", body=add_body)
check("create-member-dup-email", [400,409], s, b)

# === GYM-CODE-FLOW ===
# Look up gym by code (should work)
s,b = hit("GET", "/api/v1/gyms/by-code/INVALID-CODE", role="trainee")
check("gym-by-bad-code", [400,404], s, b)

# === CONCURRENT bookings to same slot ===
ts2 = (datetime.datetime.utcnow() + datetime.timedelta(days=3, hours=10)).replace(microsecond=0).isoformat() + "Z"
def book_same(_):
    return hit("POST","/api/v1/scheduling/sessions", role="trainee",
              body={"gymId":GYM,"trainerId":trainer_id,"sessionType":"PT","durationMinutes":60,
                    "startTime":ts2,"notes":"race"})
with ThreadPoolExecutor(max_workers=10) as ex:
    r = list(ex.map(book_same, range(10)))
codes = [x[0] for x in r]
fives = sum(1 for c in codes if c >= 500)
check("concurrent-book-same-slot-no5xx", [0], fives, f"codes={sorted(set(codes))}")

# Report
print(f"{'NAME':37} {'EXPECT':27} {'GOT':6} OK")
bugs=[]
for c in cases:
    mk = "✓" if c["ok"] else "✗"
    print(f"  {c['name']:35} {str(c['expected']):27} {str(c['got']):6} {mk}  {c['snippet']}")
    if not c["ok"]: bugs.append(c)
print(f"\n=== {len(cases)-len(bugs)}/{len(cases)} pass ===")
if bugs:
    for b in bugs: print(f"  ✗ {b['name']}: expected {b['expected']}, got {b['got']} — {b['snippet']}")
(OUT/"edge_v3_results.json").write_text(json.dumps(cases, indent=2))
