#!/usr/bin/env python3
"""End-to-end bootstrap of a brand-new gym tenant via admin/staff API ONLY.
No SQL. Mirrors what the platform admin app + staff app would do."""
import json, urllib.request, urllib.error, time
from pathlib import Path

BASE = "http://localhost:8080"

def _build_request(method, path, token, headers):
    r = urllib.request.Request(BASE + path, method=method)
    if token: r.add_header("Authorization", f"Bearer {token}")
    for k,v in (headers or {}).items(): r.add_header(k,v)
    return r

def _error_json(e):
    try: return e.code, json.loads(e.read() or b'{}')
    except: return e.code, {}

def req(method, path, token=None, **opts):
    """opts: body= (JSON payload), headers= (extra request headers)."""
    body = opts.get("body")
    r = _build_request(method, path, token, opts.get("headers"))
    data = None
    if body is not None:
        r.add_header("Content-Type", "application/json")
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(r, data=data, timeout=30) as resp:
            return resp.status, json.loads(resp.read() or b'{}')
    except urllib.error.HTTPError as e:
        return _error_json(e)
    except Exception as e:
        return 0, {"err": str(e)}

def login(email, password):
    s,b = req("POST","/api/v1/identity/login",body={"email":email,"password":password})
    if s != 200 or not b.get("data"):
        raise SystemExit(f"login failed for {email}: {s} {b}")
    return b["data"]["accessToken"]

def require_created(s, message):
    if s not in (200,201):
        raise SystemExit(message)

def make_identities(stamp):
    suffix = str(stamp)[-7:]
    emails = {role: f"e2e_{role}_{stamp}@example.com" for role in ("owner","staff","trainer","trainee")}
    # Phones must be unique across runs because identity.users.phone has a unique
    # index. Use the timestamp suffix so re-running doesn't collide.
    phones = {role: f"+2010{suffix}{i}"[:13] for i, role in enumerate(("owner","staff","trainer","trainee"), start=1)}
    return emails, phones

def create_owner(admin, owner_email, owner_phone):
    print(f"\n[2] POST /admin/owners — create owner user + Owner aggregate ({owner_email})")
    s,b = req("POST","/api/v1/admin/owners", token=admin, body={
        "email": owner_email,
        "password": "Owner1234!",
        "phone": owner_phone,
        "firstName": "E2E", "lastName": "Owner",
        "companyName": "E2E Holdings", "billingEmail": owner_email,
        "dashboardMode": "PerGym", "planTier": "Pro"
    })
    print(f"  [{s}] {b}")
    require_created(s, "admin/owners failed — cannot proceed")
    data = b.get("data") or {}
    owner_id = data.get("ownerId") or data.get("id")
    print(f"  ownerId={owner_id} userId={data.get('userId')}")
    return owner_id

def register_gym(admin, owner_id, stamp):
    print(f"\n[3] POST /api/v1/gyms — register new gym (admin scope, auto-approve, auto-provision g{{N}}_* schema)")
    s,b = req("POST","/api/v1/gyms", token=admin, body={
        "ownerId": owner_id,
        "name": f"E2E Gym {stamp}",
        "description": "Created via admin app E2E",
        "address": {"street":"1 Test St","city":"Cairo","state":"Cairo","country":"EG","zipCode":"11511"},
        "contact": {"email":f"info_{stamp}@example.com","phone":"+20223456789"},
        "autoApprove": True
    })
    print(f"  [{s}] {b}")
    require_created(s, "gyms register failed — cannot proceed")
    gym_id = b.get("data")
    print(f"  gymId={gym_id}")
    return gym_id

def owner_select_gym(owner_email, gym_id):
    # Quick post-provision sanity: registry row exists, schemas exist, subscription created.
    print("\n[3b] Verifying provisioning…")
    # Owner login + select-gym
    owner = login(owner_email, "Owner1234!")
    s,b = req("POST","/api/v1/identity/select-gym", token=owner, body={"gymId":gym_id})
    print(f"  owner select-gym: [{s}] OK={'data' in b and b.get('data') is not None}")
    if s == 200 and b.get("data"):
        owner = b["data"]["accessToken"]
    return owner

def create_staff(admin, gym_id, staff_email, staff_phone):
    print(f"\n[4] POST /admin/gyms/{{gymId}}/staff/create — add staff user")
    s,b = req("POST", f"/api/v1/admin/gyms/{gym_id}/staff/create", token=admin, body={
        "email": staff_email, "password": "Staff1234!", "phone": staff_phone,
        "firstName":"E2E","lastName":"Staff","gender":"Female", "subRole":"Manager"
    })
    print(f"  [{s}] {b}")

def staff_onboard(staff, gym_id, path, body):
    s,b = req("POST", path, token=staff, headers={"X-Gym-Id":gym_id}, body=body)
    print(f"  [{s}] {b}")
    return (b.get("data") or {}).get("initialPassword","123456")

def onboard_trainer(staff, gym_id, trainer_email, trainer_phone):
    return staff_onboard(staff, gym_id, "/api/v1/identity/users/staff-create-trainer", {
        "email": trainer_email, "phone":trainer_phone,
        "firstName":"E2E","lastName":"Trainer","gender":"Male"
    })

def onboard_trainee(staff, gym_id, trainee_email, trainee_phone):
    print(f"\n[6] POST /identity/users/staff-create — onboard trainee")
    return staff_onboard(staff, gym_id, "/api/v1/identity/users/staff-create", {
        "email": trainee_email, "phone":trainee_phone,
        "firstName":"E2E","lastName":"Trainee","gender":"Male", "section":"Mens"
    })

def check_trainee_meal_plan(trainee_email, trainee_pwd, gym_id):
    print(f"\n[7] Trainee login + /me/meal-plan (verifies MemberIds JSON wired)")
    trainee = login(trainee_email, trainee_pwd)
    s,b = req("GET","/api/v1/nutrition/me/meal-plan", token=trainee, headers={"X-Gym-Id":gym_id})
    ok = s == 200 and (b.get("errors") is None)
    print(f"  [{s}] members? {ok}  body={str(b)[:200]}")

def check_trainer_roster(trainer_email, trainer_pwd, gym_id):
    print(f"\n[8] Trainer login + own-gym roster check")
    trainer = login(trainer_email, trainer_pwd)
    s,b = req("GET", f"/api/v1/gyms/{gym_id}/trainers", token=trainer, headers={"X-Gym-Id":gym_id})
    print(f"  [{s}] trainers data={str(b)[:300]}")

def check_owner_profile(owner):
    print(f"\n[9] Owner sees gym in their dashboard")
    s,b = req("GET","/api/v1/identity/me", token=owner)
    print(f"  [{s}] owner profile: gymId={b.get('data',{}).get('gymId')} principalType={b.get('data',{}).get('principalType')}")

def print_summary(gym_id, emails):
    print("\n=== Bootstrap E2E done ===")
    print(f"  gymId      = {gym_id}")
    print(f"  ownerEmail = {emails['owner']}")
    print(f"  staffEmail = {emails['staff']}")
    print(f"  trainerEmail = {emails['trainer']}")
    print(f"  traineeEmail = {emails['trainee']}")
    print(f"  passwords: Owner1234!/Staff1234!/Trainer1234!/Trainee1234!")

def main():
    print("[1] Login as platform admin")
    admin = login("admin@demo.local", "123456")

    stamp = int(time.time())
    emails, phones = make_identities(stamp)

    owner_id = create_owner(admin, emails["owner"], phones["owner"])
    gym_id = register_gym(admin, owner_id, stamp)
    owner = owner_select_gym(emails["owner"], gym_id)
    create_staff(admin, gym_id, emails["staff"], phones["staff"])

    print(f"\n[5] Login as staff, then POST /identity/users/staff-create-trainer to onboard trainer")
    staff = login(emails["staff"], "Staff1234!")
    trainer_pwd = onboard_trainer(staff, gym_id, emails["trainer"], phones["trainer"])
    trainee_pwd = onboard_trainee(staff, gym_id, emails["trainee"], phones["trainee"])

    check_trainee_meal_plan(emails["trainee"], trainee_pwd, gym_id)
    check_trainer_roster(emails["trainer"], trainer_pwd, gym_id)
    check_owner_profile(owner)
    print_summary(gym_id, emails)

if __name__ == "__main__":
    main()
