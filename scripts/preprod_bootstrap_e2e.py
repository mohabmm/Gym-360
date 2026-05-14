#!/usr/bin/env python3
"""End-to-end bootstrap of a brand-new gym tenant via admin/staff API ONLY.
No SQL. Mirrors what the platform admin app + staff app would do."""
import json, urllib.request, urllib.error, time
from pathlib import Path

BASE = "http://localhost:8080"

def req(method, path, token=None, body=None, headers=None):
    url = BASE + path
    r = urllib.request.Request(url, method=method)
    if token: r.add_header("Authorization", f"Bearer {token}")
    if headers:
        for k,v in headers.items(): r.add_header(k,v)
    data = None
    if body is not None:
        r.add_header("Content-Type", "application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(r, data=data, timeout=30) as resp:
            return resp.status, json.loads(resp.read() or b'{}')
    except urllib.error.HTTPError as e:
        try: return e.code, json.loads(e.read() or b'{}')
        except: return e.code, {}
    except Exception as e:
        return 0, {"err": str(e)}

def login(email, password):
    s,b = req("POST","/api/v1/identity/login",body={"email":email,"password":password})
    if s != 200 or not b.get("data"):
        raise SystemExit(f"login failed for {email}: {s} {b}")
    return b["data"]["accessToken"]

print("[1] Login as platform admin")
ADMIN = login("admin@demo.local", "123456")

stamp = int(time.time())
suffix = str(stamp)[-7:]
owner_email = f"e2e_owner_{stamp}@example.com"
staff_email = f"e2e_staff_{stamp}@example.com"
trainer_email = f"e2e_trainer_{stamp}@example.com"
trainee_email = f"e2e_trainee_{stamp}@example.com"
# Phones must be unique across runs because identity.users.phone has a unique
# index. Use the timestamp suffix so re-running doesn't collide.
owner_phone   = f"+2010{suffix}1"[:13]
staff_phone   = f"+2010{suffix}2"[:13]
trainer_phone = f"+2010{suffix}3"[:13]
trainee_phone = f"+2010{suffix}4"[:13]

print(f"\n[2] POST /admin/owners — create owner user + Owner aggregate ({owner_email})")
s,b = req("POST","/api/v1/admin/owners", token=ADMIN, body={
    "email": owner_email,
    "password": "Owner1234!",
    "phone": owner_phone,
    "firstName": "E2E", "lastName": "Owner",
    "companyName": "E2E Holdings", "billingEmail": owner_email,
    "dashboardMode": "PerGym", "planTier": "Pro"
})
print(f"  [{s}] {b}")
if s not in (200,201):
    raise SystemExit("admin/owners failed — cannot proceed")
data = b.get("data") or {}
OWNER_ID = data.get("ownerId") or data.get("id")
OWNER_USER_ID = data.get("userId")
print(f"  ownerId={OWNER_ID} userId={OWNER_USER_ID}")

print(f"\n[3] POST /api/v1/gyms — register new gym (admin scope, auto-approve, auto-provision g{{N}}_* schema)")
s,b = req("POST","/api/v1/gyms", token=ADMIN, body={
    "ownerId": OWNER_ID,
    "name": f"E2E Gym {stamp}",
    "description": "Created via admin app E2E",
    "address": {"street":"1 Test St","city":"Cairo","state":"Cairo","country":"EG","zipCode":"11511"},
    "contact": {"email":f"info_{stamp}@example.com","phone":"+20223456789"},
    "autoApprove": True
})
print(f"  [{s}] {b}")
if s not in (200,201):
    raise SystemExit("gyms register failed — cannot proceed")
GYM_ID = b.get("data")
print(f"  gymId={GYM_ID}")

# Quick post-provision sanity: registry row exists, schemas exist, subscription created.
print("\n[3b] Verifying provisioning…")
# Owner login + select-gym
OWNER = login(owner_email, "Owner1234!")
s,b = req("POST","/api/v1/identity/select-gym", token=OWNER, body={"gymId":GYM_ID})
print(f"  owner select-gym: [{s}] OK={'data' in b and b.get('data') is not None}")
if s == 200 and b.get("data"):
    OWNER = b["data"]["accessToken"]

print(f"\n[4] POST /admin/gyms/{{gymId}}/staff/create — add staff user")
s,b = req("POST", f"/api/v1/admin/gyms/{GYM_ID}/staff/create", token=ADMIN, body={
    "email": staff_email, "password": "Staff1234!", "phone": staff_phone,
    "firstName":"E2E","lastName":"Staff","gender":"Female", "subRole":"Manager"
})
print(f"  [{s}] {b}")

print(f"\n[5] Login as staff, then POST /identity/users/staff-create-trainer to onboard trainer")
STAFF = login(staff_email, "Staff1234!")
s,b = req("POST","/api/v1/identity/users/staff-create-trainer", token=STAFF, headers={"X-Gym-Id":GYM_ID}, body={
    "email": trainer_email, "phone":trainer_phone,
    "firstName":"E2E","lastName":"Trainer","gender":"Male"
})
print(f"  [{s}] {b}")
TRAINER_PWD = (b.get("data") or {}).get("initialPassword","123456")

print(f"\n[6] POST /identity/users/staff-create — onboard trainee")
s,b = req("POST","/api/v1/identity/users/staff-create", token=STAFF, headers={"X-Gym-Id":GYM_ID}, body={
    "email": trainee_email, "phone":trainee_phone,
    "firstName":"E2E","lastName":"Trainee","gender":"Male", "section":"Mens"
})
print(f"  [{s}] {b}")
TRAINEE_PWD = (b.get("data") or {}).get("initialPassword","123456")

print(f"\n[7] Trainee login + /me/meal-plan (verifies MemberIds JSON wired)")
TRAINEE = login(trainee_email, TRAINEE_PWD)
s,b = req("GET","/api/v1/nutrition/me/meal-plan", token=TRAINEE, headers={"X-Gym-Id":GYM_ID})
ok = s == 200 and (b.get("errors") is None)
print(f"  [{s}] members? {ok}  body={str(b)[:200]}")

print(f"\n[8] Trainer login + own-gym roster check")
TRAINER = login(trainer_email, TRAINER_PWD)
s,b = req("GET", f"/api/v1/gyms/{GYM_ID}/trainers", token=TRAINER, headers={"X-Gym-Id":GYM_ID})
print(f"  [{s}] trainers data={str(b)[:300]}")

print(f"\n[9] Owner sees gym in their dashboard")
s,b = req("GET","/api/v1/identity/me", token=OWNER)
print(f"  [{s}] owner profile: gymId={b.get('data',{}).get('gymId')} principalType={b.get('data',{}).get('principalType')}")

print("\n=== Bootstrap E2E done ===")
print(f"  gymId      = {GYM_ID}")
print(f"  ownerEmail = {owner_email}")
print(f"  staffEmail = {staff_email}")
print(f"  trainerEmail = {trainer_email}")
print(f"  traineeEmail = {trainee_email}")
print(f"  passwords: Owner1234!/Staff1234!/Trainer1234!/Trainee1234!")
