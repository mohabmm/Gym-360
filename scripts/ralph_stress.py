#!/usr/bin/env python3
"""Iter 7 — concurrency stress on hot write endpoints. No 5xx allowed."""
import json, urllib.request, urllib.error, urllib.parse, time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"
GYM = (OUT / "gym_id.txt").read_text().strip()
JWTS = {r: (OUT / f"jwt_{r}.txt").read_text().strip() for r in ["admin","owner","staff","trainer","trainee"]}

def hit(method, path, role=None, body=None, query=None):
    url = BASE + path
    if query: url += "?" + urllib.parse.urlencode(query)
    req = urllib.request.Request(url, method=method.upper())
    if role: req.add_header("Authorization", f"Bearer {JWTS[role]}")
    req.add_header("X-Gym-Id", GYM)
    data = None
    if body is not None and method.lower() in ("post","put","patch"):
        req.add_header("Content-Type","application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data=data, timeout=30) as r:
            return r.status, r.read().decode("utf-8","replace")[:200]
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8","replace")[:200]
    except Exception as e:
        return 0, f"{type(e).__name__}: {e}"

def stress(name, fn, n=30):
    with ThreadPoolExecutor(max_workers=15) as ex:
        r = list(ex.map(fn, range(n)))
    codes = [x[0] for x in r]
    fives = [c for c in codes if c >= 500 or c == 0]
    dist = {}
    for c in codes: dist[c] = dist.get(c,0)+1
    print(f"  {name:40} n={n} dist={dist} 5xx_or_0={len(fives)}", flush=True)
    return len(fives) == 0

results = []

# Water-log write storm
def water(_): return hit("POST","/api/v1/nutrition/water-log", role="trainee", body={"amountMl":100,"loggedAt":"2026-05-12T13:00:00Z"})
results.append(("water-log-30x", stress("water-log POST 30x", water, 30)))

# Body metric write storm
def bm(_): return hit("POST","/api/v1/progress/body-metrics", role="trainee", body={"gymId":GYM,"weightKg":75.5,"recordedAt":"2026-05-12T12:00:00Z"})
results.append(("body-metric-30x", stress("body-metric POST 30x", bm, 30)))

# List read storm
def list_members(_): return hit("GET", f"/api/v1/gyms/{GYM}/members", role="owner", query={"page":1,"pageSize":20})
results.append(("list-members-50x", stress("list members 50x parallel", list_members, 50)))

# Trainer ratings list
def list_ratings(_): return hit("GET", f"/api/v1/gamification/trainers/5452d4d9-3ef2-4e0f-ae57-bf19956a824b/ratings", role="trainee", query={"page":1,"pageSize":10})
results.append(("trainer-ratings-50x", stress("trainer-ratings 50x", list_ratings, 50)))

# Auth login storm (BCrypt is expensive — does it serialize?)
def login_admin(_): return hit("POST","/api/v1/identity/login", body={"email":"admin@demo.local","password":"123456"})
results.append(("login-20x", stress("login admin 20x parallel", login_admin, 20)))

# Refresh /me storm
def me(_): return hit("GET","/api/v1/identity/me", role="trainee")
results.append(("me-50x", stress("identity/me 50x", me, 50)))

# Available slots storm
def slots(_): return hit("GET","/api/v1/scheduling/available-slots", role="trainee",
                         query={"gymId":GYM,"trainerId":"5452d4d9-3ef2-4e0f-ae57-bf19956a824b","date":"2026-05-15","durationMinutes":60})
results.append(("slots-30x", stress("available-slots 30x", slots, 30)))

# Mixed-role parallel
def mixed(i):
    roles = ["admin","owner","staff","trainer","trainee"]
    role = roles[i % len(roles)]
    return hit("GET","/api/v1/identity/me", role=role)
results.append(("mixed-roles-50x", stress("identity/me mixed roles 50x", mixed, 50)))

# Owner dashboard storm
def owner_dash(_): return hit("GET","/api/v1/owners/e8e1dc31-ff43-4997-9ff4-28f3e96aad05/dashboard", role="owner")
results.append(("owner-dashboard-20x", stress("owner dashboard 20x", owner_dash, 20)))

print("\n=== Summary ===")
pass_n = sum(1 for _,ok in results if ok)
print(f"{pass_n}/{len(results)} stress tests passed (zero 5xx/0)")
fail = [n for n,ok in results if not ok]
if fail:
    print("FAIL:", fail)
