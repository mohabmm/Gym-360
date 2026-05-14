#!/usr/bin/env python3
"""Iter 6 — per-module deep sweep with realistic data per role.
Drives each module's surface with happy-path payloads to maximize 2xx and surface 5xx."""
import json, urllib.request, urllib.error, urllib.parse, time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "test_logs" / "preprod"
GYM = (OUT / "gym_id.txt").read_text().strip()
JWTS = {r: (OUT / f"jwt_{r}.txt").read_text().strip() for r in ["admin","owner","staff","trainer","trainee"]}
TRAINER = "5452d4d9-3ef2-4e0f-ae57-bf19956a824b"
TRAINEE = "f89ca860-23b9-4f01-a8ec-7bc4aaccff87"

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
            return r.status, r.read().decode("utf-8","replace")[:300]
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8","replace")[:300]
    except Exception as e:
        return 0, f"{type(e).__name__}: {e}"

results = []
def call(name, method, path, role, **kw):
    s,b = hit(method, path, role=role, **kw)
    results.append((name, method, path, role, s, b))
    return s, b

# === COMMUNICATION ===
call("comm.list-conversations","GET","/api/v1/communication/conversations","trainee", query={"page":1,"pageSize":10})
s,b = call("comm.start-conversation","POST","/api/v1/communication/conversations","trainee",
           body={"gymId":GYM,"recipientId":TRAINER,"initialMessage":"hi from sweep"})
conv_id = None
try:
    d = json.loads(b).get("data")
    conv_id = d.get("id") if isinstance(d, dict) else (d if isinstance(d, str) else None)
except: pass
call("comm.list-announcements","GET","/api/v1/communication/announcements","trainee", query={"page":1,"pageSize":10})

# Send message to conversation
if conv_id:
    call("comm.send-message","POST",f"/api/v1/communication/conversations/{conv_id}/messages","trainee",
         body={"content":"second message"})
    call("comm.get-messages","GET",f"/api/v1/communication/conversations/{conv_id}/messages","trainee",
         query={"page":1,"pageSize":10})

# === GAMIFICATION ===
call("gami.leaderboards","GET",f"/api/v1/gamification/leaderboards","trainee",
     query={"gymId":GYM,"category":"workouts","timeframe":"month"})
call("gami.my-points","GET","/api/v1/gamification/my-points","trainee")
call("gami.trainer-ratings","GET",f"/api/v1/gamification/trainers/{TRAINER}/ratings","trainee",
     query={"page":1,"pageSize":10})
call("gami.coach-of-month","GET",f"/api/v1/gamification/coach-of-the-month","trainee",
     query={"gymId":GYM,"year":2026,"month":5})
call("gami.opt-out","POST","/api/v1/gamification/leaderboards/opt-out","trainee",
     body={"gymId":GYM,"reason":"sweep"})
call("gami.opt-in","POST","/api/v1/gamification/leaderboards/opt-in","trainee", body={"gymId":GYM})

# === BILLING ===
call("billing.me.packages","GET","/api/v1/billing/me/packages","trainee")
call("billing.me.payments","GET","/api/v1/billing/me/payments","trainee", query={"page":1,"pageSize":10})
call("billing.plans","GET","/api/v1/admin/plans","admin")
call("billing.gym-subscription","GET",f"/api/v1/admin/gyms/{GYM}/subscription","admin")
call("billing.feature-overrides","GET",f"/api/v1/admin/gyms/{GYM}/feature-overrides","admin")
call("billing.gym-storage","GET",f"/api/v1/admin/gyms/{GYM}/storage","admin")

# === PROGRESS ===
call("progress.body-metrics","GET","/api/v1/progress/body-metrics/me","trainee", query={"page":1,"pageSize":10})
call("progress.log-body","POST","/api/v1/progress/body-metrics","trainee",
     body={"gymId":GYM,"weightKg":75,"bodyFatPct":15,"recordedAt":"2026-05-12T12:00:00Z"})
call("progress.photos","GET","/api/v1/progress/photos/me","trainee", query={"page":1,"pageSize":10})

# === IDENTITY ===
call("id.me","GET","/api/v1/identity/me","trainee")
call("id.update-profile","PUT","/api/v1/identity/me","trainee",
     body={"firstName":"Sweep","lastName":"Updated","phoneNumber":"+201234567890"})
call("id.change-pwd","POST","/api/v1/identity/me/change-password","trainee",
     body={"currentPassword":"wrong","newPassword":"NewPass123!"})
call("id.notification-prefs","GET","/api/v1/identity/me/notification-preferences","trainee")

# === SCHEDULING ===
call("sched.available-slots","GET","/api/v1/scheduling/available-slots","trainee",
     query={"gymId":GYM,"trainerId":TRAINER,"date":"2026-05-15","durationMinutes":60})
call("sched.my-sessions","GET","/api/v1/scheduling/sessions","trainee", query={"page":1,"pageSize":10})
call("sched.upcoming","GET","/api/v1/scheduling/sessions/upcoming","trainee")
call("sched.stats","GET","/api/v1/scheduling/sessions/stats","trainer")
call("sched.availability","GET","/api/v1/scheduling/availability","trainer", query={"gymId":GYM,"trainerId":TRAINER})

# === TRAINING ===
call("train.exercises","GET","/api/v1/training/exercises","trainer", query={"page":1,"pageSize":10})
call("train.workouts","GET","/api/v1/training/workouts","trainer", query={"page":1,"pageSize":10})
call("train.templates","GET","/api/v1/training/templates","trainer")
call("train.programs","GET","/api/v1/training/programs","trainer", query={"page":1,"pageSize":10})
call("train.todays-workout","GET","/api/v1/training/todays-workout","trainee")
call("train.workout-logs","GET","/api/v1/training/workout-logs","trainee", query={"page":1,"pageSize":10})

# === ADMIN ===
call("admin.users","GET","/api/v1/admin/users","admin", query={"page":1,"pageSize":10})
call("admin.owners","GET","/api/v1/admin/owners","admin", query={"page":1,"pageSize":10})
call("admin.features","GET","/api/v1/admin/features","admin")
call("admin.feature-matrix","GET","/api/v1/admin/plans/feature-matrix","admin")
call("admin.platform-config","GET","/api/v1/admin/platform-config","admin")
call("admin.security-settings","GET","/api/v1/admin/security-settings","admin")
call("admin.analytics","GET","/api/v1/admin/analytics","admin")
call("admin.analytics-churn","GET","/api/v1/admin/analytics/churn","admin")
call("admin.plan-change-log","GET","/api/v1/admin/plan-change-log","admin", query={"page":1,"pageSize":10})

# === OWNERS ===
call("owners.dashboard","GET",f"/api/v1/owners/e8e1dc31-ff43-4997-9ff4-28f3e96aad05/dashboard","owner")
call("owners.gyms","GET",f"/api/v1/owners/e8e1dc31-ff43-4997-9ff4-28f3e96aad05/gyms","owner")

# === PDPL ===
call("pdpl.consent","GET","/api/v1/pdpl/consent","trainee")
call("pdpl.export","POST","/api/v1/pdpl/export","trainee", body={})

# Summary
buckets = {}
fives = []
auths = []
for name,m,p,role,s,b in results:
    buckets.setdefault(s,0); buckets[s]+=1
    if s >= 500 or s == 0: fives.append((name,m,p,role,s,b[:150]))
    if s in (401,403): auths.append((name,m,p,role,s,b[:120]))

print(f"{'NAME':30} {'METHOD':6} PATH/ROLE                              STATUS")
for name,m,p,role,s,b in results:
    mark = "OK" if 200<=s<300 else "WARN" if s<500 else "BUG"
    print(f"  [{s:>3}] {name:30} {m:6} {p[:55]:55} ({role:7}) {mark}")
print(f"\n=== distribution: {buckets} ===")
if fives:
    print("\nCRITICAL (5xx/0):")
    for x in fives: print(f"  {x[0]}: {x[4]} — {x[5]}")
if auths:
    print("\nAuth (401/403):")
    for x in auths: print(f"  {x[0]}: {x[4]} ({x[3]}) — {x[5]}")
(OUT/"module_sweep.json").write_text(json.dumps([{"name":n,"method":m,"path":p,"role":r,"status":s,"snippet":b[:200]} for n,m,p,r,s,b in results], indent=2))
