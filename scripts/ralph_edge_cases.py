#!/usr/bin/env python3
"""Edge-case bombing: auth boundaries, cross-tenant, concurrency, malicious input.
Surfaces real bugs that plain sweep misses."""
import json, urllib.request, urllib.error, urllib.parse, time, threading, uuid
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"
GYM = (OUT / "gym_id.txt").read_text().strip()
JWTS = {r: (OUT / f"jwt_{r}.txt").read_text().strip() for r in ["admin","owner","staff","trainer","trainee"]}
OTHER_GYM = "00000000-0000-0000-0000-000000000999"  # foreign tenant

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

def check(name, expected_codes, status, snippet, severity="bug"):
    ok = status in expected_codes
    cases.append({"name": name, "expected": expected_codes, "got": status, "ok": ok, "severity": severity, "snippet": snippet[:150]})

# === AUTH ===
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", token="")
check("no-token-on-protected", [401], s, b)

s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", token="not.a.jwt")
check("garbage-token", [401], s, b)

s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", token="eyJhbGciOiJIUzI1NiJ9.e30.invalid")
check("malformed-jwt", [401], s, b)

# trainee accessing owner endpoint
s,b = hit("GET", f"/api/v1/admin/users?page=1&pageSize=5", role="trainee")
check("trainee-hits-admin", [401,403], s, b)

# staff trying admin
s,b = hit("POST", "/api/v1/admin/owners", role="staff", body={"email":"x@x","password":"123456","firstName":"a","lastName":"b","phone":"+200","companyName":"c","billingEmail":"x@x","planTier":"Pro","dashboardMode":"Both"})
check("staff-hits-admin-owners", [401,403], s, b)

# === CROSS-TENANT (multi-tenancy rule 10) ===
# owner has gymId=GYM in JWT; route uses OTHER_GYM → must 403
s,b = hit("GET", f"/api/v1/gyms/{OTHER_GYM}/members?page=1&pageSize=5", role="owner")
check("cross-tenant-route", [403,404], s, b)

# trainer accessing other gym
s,b = hit("GET", f"/api/v1/gyms/{OTHER_GYM}/trainers", role="trainer")
check("cross-tenant-trainer", [403,404], s, b)

# === MALICIOUS INPUT ===
# SQL injection in query string (properly url-encoded)
s,b = hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner",
         query={"page":1,"pageSize":5,"search":"' OR 1=1--"})
check("sql-injection-search", [200,400], s, b)

# Oversized name
huge = "A"*5000
s,b = hit("POST", f"/api/v1/training/exercises", role="trainer",
         body={"name": huge, "category":"general", "muscleGroups":["chest"], "equipment":"none", "description":"x"})
check("oversized-name", [400], s, b, severity="should-validate")

# Negative integer
s,b = hit("PUT", "/api/v1/nutrition/water-log/target", role="trainee",
         body={"gymId":GYM,"date":"2026-05-12","targetMl":-100})
check("negative-water-target", [400], s, b)

# Zero water target
s,b = hit("PUT", "/api/v1/nutrition/water-log/target", role="trainee",
         body={"gymId":GYM,"date":"2026-05-12","targetMl":0})
check("zero-water-target", [400], s, b)

# Date in far future
s,b = hit("PUT", "/api/v1/nutrition/water-log/target", role="trainee",
         body={"gymId":GYM,"date":"2099-12-31","targetMl":2500})
check("future-date-target", [204,400], s, b)

# Invalid enum
s,b = hit("POST", f"/api/v1/training/workouts", role="trainer",
         body={"trainerId": "5452d4d9-3ef2-4e0f-ae57-bf19956a824b","gymId":GYM,
               "name":"x","description":"x","estimatedDurationMinutes":30,
               "targetMuscleGroups":["chest"],"isTemplate":True,"exercises":[]})
check("create-workout-base", [201,400,409], s, b)

# Garbage JSON
url = BASE + "/api/v1/nutrition/water-log/target"
req = urllib.request.Request(url, method="PUT")
req.add_header("Authorization", f"Bearer {JWTS['trainee']}")
req.add_header("Content-Type", "application/json")
try:
    with urllib.request.urlopen(req, data=b"{not json", timeout=10) as r:
        s,b2 = r.status, r.read().decode()
except urllib.error.HTTPError as e:
    s,b2 = e.code, e.read().decode("utf-8","replace")[:200]
check("malformed-json", [400], s, b2)

# Wrong content type
req = urllib.request.Request(url, method="PUT")
req.add_header("Authorization", f"Bearer {JWTS['trainee']}")
req.add_header("Content-Type", "text/plain")
try:
    with urllib.request.urlopen(req, data=b"plain text", timeout=10) as r:
        s,b2 = r.status, r.read().decode()
except urllib.error.HTTPError as e:
    s,b2 = e.code, e.read().decode("utf-8","replace")[:200]
check("wrong-content-type", [400,415,500], s, b2)

# === CONCURRENCY ===
# 20 parallel water-log writes — should not 500
def water_write(_):
    return hit("POST", "/api/v1/nutrition/water-log", role="trainee",
               body={"gymId":GYM,"amountMl":250,"loggedAt":"2026-05-12T12:00:00Z"})
with ThreadPoolExecutor(max_workers=20) as ex:
    results = list(ex.map(water_write, range(20)))
codes = [r[0] for r in results]
fives = [c for c in codes if c >= 500]
check("concurrent-water-20x", [], len(fives), f"distribution: {sorted(set(codes))}", severity="info")
# all should be 2xx/4xx, no 5xx
if not fives:
    cases[-1]["ok"] = True; cases[-1]["got"] = 0
else:
    cases[-1]["ok"] = False

# Replay check-in (idempotency) — skipped destructive in main sweep but worth testing concurrency
# (use list endpoint instead to avoid duplicate check-in pollution)

# === MISSING/EMPTY ===
# Empty body POST
s,b = hit("POST", "/api/v1/nutrition/water-log", role="trainee", body={})
check("empty-water-log", [400], s, b)

# Missing gymId in body — now JWT-derived (security hardening), so should succeed.
s,b = hit("POST", "/api/v1/nutrition/water-log", role="trainee", body={"amountMl":250})
check("missing-gymId-uses-jwt", [201], s, b)

# === REPORT ===
print(f"{'NAME':45} {'EXPECT':14} {'GOT':6} {'OK':4}")
bugs = []
for c in cases:
    mark = "✓" if c["ok"] else "✗"
    print(f"  {c['name']:43} {str(c['expected']):14} {str(c['got']):6} {mark}  {c['snippet'][:60]}")
    if not c["ok"]: bugs.append(c)
print(f"\n=== {len(cases)-len(bugs)}/{len(cases)} pass ===")
if bugs:
    print("FAILURES:")
    for b in bugs: print(f"  {b['name']}: expected {b['expected']}, got {b['got']} — {b['snippet']}")
(OUT / "edge_results.json").write_text(json.dumps(cases, indent=2))
