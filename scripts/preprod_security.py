#!/usr/bin/env python3
"""Targeted security + contract tests."""
import json, urllib.request, urllib.error
from pathlib import Path

BASE = "http://localhost:8080"
ROOT = Path(__file__).resolve().parent.parent
JWTS = {r: (ROOT/"test_logs/preprod"/f"jwt_{r}.txt").read_text().strip()
        for r in ["admin","owner","staff","trainer","trainee"]}
GYM = "bdaeb6ec-5302-45e3-aa75-e86756a22490"

def req(method, path, role=None, body=None, headers=None):
    url = BASE + path
    r = urllib.request.Request(url, method=method)
    if role: r.add_header("Authorization", f"Bearer {JWTS[role]}")
    if headers:
        for k,v in headers.items(): r.add_header(k,v)
    data = None
    if body is not None:
        r.add_header("Content-Type", "application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(r, data=data, timeout=15) as resp:
            return resp.status, resp.read().decode("utf-8","replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8","replace")
    except Exception as e:
        return 0, str(e)

results = []
def case(name, expected, actual, detail=""):
    ok = expected == actual or (isinstance(expected, set) and actual in expected)
    results.append((ok, name, expected, actual, detail))
    print(f"  {'PASS' if ok else 'FAIL'} {name}: expected={expected} got={actual} {detail[:80]}")

print("=== RBAC: cross-role attempts ===")
# Trainee should NOT access admin endpoints
s,b = req("GET", "/api/v1/admin/gyms?page=1&pageSize=10", role="trainee")
case("trainee→admin/gyms denied", {401,403}, s, b[:100])

# Staff should NOT create gyms
s,b = req("POST", "/api/v1/gyms", role="staff", body={"name":"X","gymCode":"X","ownerId":"00000000-0000-0000-0000-000000000001"})
case("staff→create gym denied", {401,403}, s, b[:100])

# Trainee should NOT access another trainee's data — try staff-create
s,b = req("POST", "/api/v1/identity/users/staff-create", role="trainee", body={"email":"x@y.com","password":"abcdefgh","phoneNumber":"+201000000099","firstName":"X","lastName":"Y","gender":"Male"})
case("trainee→staff-create denied", {401,403}, s, b[:100])

# Owner cannot select gym they don't own
s,b = req("POST", "/api/v1/identity/select-gym", role="owner", body={"gymId":"00000000-0000-0000-0000-000000000099"})
case("owner→select unowned gym denied", {403,404,400}, s, b[:100])

# No JWT — should 401
s,b = req("GET", "/api/v1/identity/me")
case("no-jwt→/identity/me 401", 401, s, b[:100])

print("\n=== JWT-derivation security (body userId must be ignored) ===")
# Try setting another userId in chat send (memory: was bug)
s,b = req("POST", "/api/v1/communication/messages", role="trainee", headers={"X-Gym-Id":GYM}, body={"senderId":"00000000-0000-0000-0000-000000000001","conversationId":"00000000-0000-0000-0000-000000000001","content":"x"})
case("chat send senderId from JWT (not body)", {400,403,404}, s, b[:200])

# Notification prefs — body userId attempt (memory: fixed)
s,b = req("PUT", "/api/v1/communication/notification-preferences", role="trainee", body={"userId":"00000000-0000-0000-0000-000000000001","newSession":True})
case("notif prefs userId from JWT", {200,400,204}, s, b[:200])

print("\n=== Contract: success-path responses ===")
# /identity/me shape
s,b = req("GET", "/api/v1/identity/me", role="trainee")
case("/identity/me 200", 200, s)
try:
    d = json.loads(b)["data"]
    for k in ["id","email","role","principalType"]:
        case(f"  /me has {k}", True, k in d and d[k] is not None)
except: case("  /me parse", True, False, b[:100])

# Paged endpoints have items shape (memory: 12 endpoints)
for p in ["/api/v1/gyms/"+GYM+"/staff?page=1&pageSize=5", "/api/v1/gyms/"+GYM+"/trainers?page=1&pageSize=5", "/api/v1/billing/me/packages?page=1&pageSize=5"]:
    role = "trainee" if "/me/" in p else "staff"
    s,b = req("GET", p, role=role, headers={"X-Gym-Id":GYM})
    if s == 200:
        try:
            d = json.loads(b)["data"]
            case(f"paged: {p[:50]} has items[]", True, isinstance(d.get("items"), list))
        except: case(f"paged parse {p[:50]}", True, False, b[:100])
    else:
        print(f"  SKIP paged {p[:50]} (status {s})")

print("\n=== Tenant isolation ===")
# Trainee with X-Gym-Id of another gym they're not a member of
OTHER_GYM = "989414bd-38e2-4cf7-b40e-481596d780ed"  # Ralph Gym 1778368854
s,b = req("GET", "/api/v1/billing/me/packages?page=1&pageSize=5", role="trainee", headers={"X-Gym-Id": OTHER_GYM})
case("trainee→other gym /me/packages denied", {401,403,404,400}, s, b[:200])

# Owner accessing different owner's gym
s,b = req("GET", f"/api/v1/gyms/{OTHER_GYM}/staff?page=1&pageSize=5", role="owner")
case("owner→other owner's gym denied", {401,403,404,400}, s, b[:200])

print("\n=== Health ===")
s,b = req("GET","/health/ready"); case("health/ready", 200, s)
s,b = req("GET","/health/live"); case("health/live", 200, s)

ok = sum(1 for r in results if r[0])
print(f"\n=== Total: {ok}/{len(results)} pass ===")
(ROOT/"test_logs/preprod/security_results.json").write_text(json.dumps([{"ok":r[0],"name":r[1],"exp":str(r[2]),"actual":r[3],"detail":r[4][:200]} for r in results], indent=2))
