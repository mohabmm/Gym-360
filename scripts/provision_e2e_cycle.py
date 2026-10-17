#!/usr/bin/env python3
"""Provision fresh accounts for a 200-flow E2E cycle.

Chain: admin login -> register gym (autoApprove) -> wait for Pro plan ->
       create owner -> create staff (Manager) -> create trainer ->
       create trainee. Each role re-logs after creation; owner+staff+trainer
       call /identity/select-gym. Dumps creds to tests/e2e_cycle/creds_<ts>.json.
"""
import json
import os
import sys
import time
import uuid
from pathlib import Path
import urllib.request
import urllib.error

API = os.environ.get("API_BASE", "http://localhost:8080/api/v1")
OUT_DIR = Path(__file__).resolve().parent.parent / "tests" / "e2e_cycle"
OUT_DIR.mkdir(parents=True, exist_ok=True)
TS = int(time.time())
TAG = f"e2e_{TS}"
DEFAULT_PWD = "Test@1234"


REQUEST_TIMEOUT = 15
PRO_PLAN_ID = "00000000-0000-0000-0001-000000000003"


def _decode_error(e):
    body = e.read()
    try:
        return e.code, json.loads(body)
    except Exception:
        return e.code, body.decode(errors="replace")


def req(method, path, body=None, token=None):
    url = path if path.startswith("http") else f"{API}{path}"
    data = json.dumps(body).encode() if body is not None else None
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(r, timeout=REQUEST_TIMEOUT) as resp:
            body = resp.read()
            return resp.status, json.loads(body) if body else None
    except urllib.error.HTTPError as e:
        return _decode_error(e)


def expect(name, code, expected, payload):
    ok = code == expected or (isinstance(expected, list) and code in expected)
    print(f"[{'OK' if ok else 'XX'}] {name}: {code}")
    if not ok:
        print("  payload:", str(payload)[:400])
        sys.exit(2)
    return payload


def token_of(payload):
    return payload["data"]["accessToken"]


def login(label, email, password):
    code, p = req("POST", "/identity/login", {"email": email, "password": password})
    expect(label, code, 200, p)
    return token_of(p)


def try_select_gym(token, gym_id):
    """select-gym; keep the old token when the call is not 200."""
    code, p = req("POST", "/identity/select-gym", {"gymId": gym_id}, token=token)
    return token_of(p) if code == 200 else token


def initial_password(p):
    pwd = p["data"].get("initialPassword") if isinstance(p["data"], dict) else None
    return pwd or DEFAULT_PWD


def extract_gym_id(data):
    return data["gymId"] if isinstance(data, dict) and "gymId" in data else data


def admin_login(creds):
    # 1. admin login
    admin_token = login("admin login", "admin@demo.local", "123456")
    creds["accounts"]["admin"] = {"email": "admin@demo.local", "token": admin_token}
    return admin_token


def create_owner(admin_token, owner_email):
    # 2. create owner via /admin/owners (provisions User+Owner+Contract, links user.owner_id)
    code, p = req("POST", "/admin/owners", {
        "email": owner_email, "password": DEFAULT_PWD,
        "firstName": "OwnerE2E", "lastName": TAG,
        "phone": "+201000000010",
        "companyName": f"E2E Co {TS}", "billingEmail": owner_email,
        "planTier": "Pro", "dashboardMode": "Both",
    }, token=admin_token)
    expect("create owner", code, [200, 201], p)
    owner_user_id = p["data"]["userId"]
    owner_aggregate_id = p["data"]["ownerId"]
    print(f"  owner_user_id={owner_user_id} owner_aggregate_id={owner_aggregate_id}")
    return owner_aggregate_id  # gym ownerId field expects Owner.Id


def register_gym(creds, admin_token, owner_id):
    # 3. register gym autoApprove
    code, p = req("POST", "/gyms", {
        "ownerId": owner_id,
        "name": f"E2E Gym {TS}",
        "description": "E2E cycle gym",
        "address": {"street": "1 Test St", "city": "Cairo", "state": "Cairo", "country": "EG", "zipCode": "11111", "latitude": 30.04, "longitude": 31.23},
        "contact": {"email": f"contact_{TAG}@e2e.local", "phone": "+201000000000", "website": "https://e2e.local"},
        "autoApprove": True,
    }, token=admin_token)
    expect("register gym", code, [200, 201], p)
    gym_id = extract_gym_id(p["data"])
    creds["gym"] = {"id": gym_id, "name": f"E2E Gym {TS}"}
    print(f"  gym_id={gym_id}")
    return gym_id


def _plan_data(code, p):
    return p.get("data") if code == 200 and p else None


def wait_for_plan(admin_token, gym_id):
    # 4. poll plan event applied (Pro subscription expected via GymApprovedIntegrationEvent)
    deadline = time.time() + 30
    plan_seen = None
    while time.time() < deadline:
        code, p = req("GET", f"/billing/plans?gymId={gym_id}", token=admin_token)
        plan_seen = _plan_data(code, p)
        if plan_seen:
            break
        time.sleep(1)
    print(f"  plans-after-event: {'yes' if plan_seen else 'no'}")


def owner_session(creds, owner_email, owner_id, gym_id):
    # 5. owner login + select-gym
    owner_token = login("owner login", owner_email, DEFAULT_PWD)
    code, p = req("POST", "/identity/select-gym", {"gymId": gym_id}, token=owner_token)
    expect("owner select-gym", code, 200, p)
    owner_token = token_of(p)
    creds["accounts"]["owner"] = {"email": owner_email, "password": DEFAULT_PWD, "id": owner_id, "token": owner_token}
    return owner_token


def create_staff(creds, admin_token, gym_id):
    # 6. create staff (Manager) via admin endpoint
    staff_email = f"staff_{TAG}@e2e.local"
    code, p = req("POST", f"/admin/gyms/{gym_id}/staff/create", {
        "email": staff_email, "password": DEFAULT_PWD,
        "firstName": "StaffE2E", "lastName": TAG,
        "phone": "+201000000001", "staffSubRole": "manager",
    }, token=admin_token)
    expect("create staff", code, [200, 201], p)
    staff_token = try_select_gym(login("staff login", staff_email, DEFAULT_PWD), gym_id)
    creds["accounts"]["staff"] = {"email": staff_email, "password": DEFAULT_PWD, "token": staff_token}
    return staff_token


def create_trainer(creds, staff_token, gym_id):
    # 7. trainer via staff-create-trainer (needs staff JWT with gym selected)
    trainer_email = f"trainer_{TAG}@e2e.local"
    code, p = req("POST", "/identity/users/staff-create-trainer", {
        "firstName": "TrainerE2E", "lastName": TAG,
        "phone": "+201000000002", "email": trainer_email,
    }, token=staff_token)
    expect("create trainer", code, [200, 201], p)
    trainer_pwd = initial_password(p)
    trainer_token = try_select_gym(login("trainer login", trainer_email, trainer_pwd), gym_id)
    creds["accounts"]["trainer"] = {"email": trainer_email, "password": trainer_pwd, "token": trainer_token}
    return trainer_email


def upgrade_to_pro(admin_token, gym_id):
    # 7b. Upgrade gym to Pro plan so feature-gated flows work
    code, _ = req("PUT", f"/admin/gyms/{gym_id}/subscription/plan",
                  {"planId": PRO_PLAN_ID, "reason": "e2e cycle"}, token=admin_token)
    print(f"  upgrade-to-Pro: {code}")


def _user_items(p):
    items = p.get("data") or []
    if isinstance(items, dict):
        items = items.get("items") or []
    return items


def find_user_id(admin_token, email):
    code, p = req("GET", f"/admin/users?search={email}", token=admin_token)
    if code != 200:
        return None
    return next((u.get("id") for u in _user_items(p) if u.get("email") == email), None)


def attach_trainer(creds, owner_token, gym_id, trainer_user_id):
    if not trainer_user_id:
        return
    code, _ = req("POST", f"/gyms/{gym_id}/trainers",
                  {"trainerId": trainer_user_id, "bio": "e2e", "specializations": ["strength"]},
                  token=owner_token)
    print(f"  add-trainer-to-gym: {code}")
    code, _ = req("POST", f"/gyms/{gym_id}/trainers/{trainer_user_id}/approve", {}, token=owner_token)
    print(f"  approve-trainer: {code}")
    creds["trainer_id"] = trainer_user_id


def create_trainee(creds, staff_token):
    # 8. trainee via staff-create
    trainee_email = f"trainee_{TAG}@e2e.local"
    code, p = req("POST", "/identity/users/staff-create", {
        "firstName": "TraineeE2E", "lastName": TAG,
        "phone": "+201000000003", "email": trainee_email, "section": "general",
    }, token=staff_token)
    expect("create trainee", code, [200, 201], p)
    trainee_pwd = initial_password(p)
    trainee_token = login("trainee login", trainee_email, trainee_pwd)
    creds["accounts"]["trainee"] = {"email": trainee_email, "password": trainee_pwd, "token": trainee_token}


def write_creds(creds):
    out = OUT_DIR / f"creds_{TS}.json"
    out.write_text(json.dumps(creds, indent=2))
    latest = OUT_DIR / "creds_latest.json"
    latest.write_text(json.dumps(creds, indent=2))
    print(f"OK  wrote {out}")
    print(f"OK  wrote {latest}")


def main():
    creds = {"tag": TAG, "ts": TS, "api": API, "accounts": {}}
    admin_token = admin_login(creds)
    owner_email = f"owner_{TAG}@e2e.local"
    owner_id = create_owner(admin_token, owner_email)
    gym_id = register_gym(creds, admin_token, owner_id)
    wait_for_plan(admin_token, gym_id)
    owner_token = owner_session(creds, owner_email, owner_id, gym_id)
    staff_token = create_staff(creds, admin_token, gym_id)
    trainer_email = create_trainer(creds, staff_token, gym_id)
    upgrade_to_pro(admin_token, gym_id)
    # 7c. Look up trainer user id, add to gym, approve so it lists
    trainer_user_id = find_user_id(admin_token, trainer_email)
    attach_trainer(creds, owner_token, gym_id, trainer_user_id)
    create_trainee(creds, staff_token)
    write_creds(creds)


if __name__ == "__main__":
    main()
