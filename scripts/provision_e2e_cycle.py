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


def req(method, path, body=None, token=None, timeout=15):
    url = path if path.startswith("http") else f"{API}{path}"
    data = json.dumps(body).encode() if body is not None else None
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(r, timeout=timeout) as resp:
            body = resp.read()
            return resp.status, json.loads(body) if body else None
    except urllib.error.HTTPError as e:
        body = e.read()
        try:
            return e.code, json.loads(body)
        except Exception:
            return e.code, body.decode(errors="replace")


def expect(name, code, expected, payload):
    ok = code == expected or (isinstance(expected, list) and code in expected)
    print(f"[{'OK' if ok else 'XX'}] {name}: {code}")
    if not ok:
        print("  payload:", str(payload)[:400])
        sys.exit(2)
    return payload


def token_of(payload):
    return payload["data"]["accessToken"]


def main():
    creds = {"tag": TAG, "ts": TS, "api": API, "accounts": {}}

    # 1. admin login
    code, p = req("POST", "/identity/login", {"email": "admin@demo.local", "password": "123456"})
    expect("admin login", code, 200, p)
    admin_token = token_of(p)
    creds["accounts"]["admin"] = {"email": "admin@demo.local", "token": admin_token}

    # 2. create owner via /admin/owners (provisions User+Owner+Contract, links user.owner_id)
    owner_email = f"owner_{TAG}@e2e.local"
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
    owner_id = owner_aggregate_id  # gym ownerId field expects Owner.Id

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
    gym_id = p["data"]["gymId"] if isinstance(p["data"], dict) and "gymId" in p["data"] else p["data"]
    creds["gym"] = {"id": gym_id, "name": f"E2E Gym {TS}"}
    print(f"  gym_id={gym_id}")

    # 4. poll plan event applied (Pro subscription expected via GymApprovedIntegrationEvent)
    deadline = time.time() + 30
    plan_seen = None
    while time.time() < deadline:
        code, p = req("GET", f"/billing/plans?gymId={gym_id}", token=admin_token)
        if code == 200 and p and p.get("data"):
            plan_seen = p["data"]
            break
        time.sleep(1)
    print(f"  plans-after-event: {'yes' if plan_seen else 'no'}")

    # 5. owner login + select-gym
    code, p = req("POST", "/identity/login", {"email": owner_email, "password": DEFAULT_PWD})
    expect("owner login", code, 200, p)
    owner_token = token_of(p)
    code, p = req("POST", "/identity/select-gym", {"gymId": gym_id}, token=owner_token)
    expect("owner select-gym", code, 200, p)
    owner_token = token_of(p)
    creds["accounts"]["owner"] = {"email": owner_email, "password": DEFAULT_PWD, "id": owner_id, "token": owner_token}

    # 6. create staff (Manager) via admin endpoint
    staff_email = f"staff_{TAG}@e2e.local"
    code, p = req("POST", f"/admin/gyms/{gym_id}/staff/create", {
        "email": staff_email, "password": DEFAULT_PWD,
        "firstName": "StaffE2E", "lastName": TAG,
        "phone": "+201000000001", "staffSubRole": "manager",
    }, token=admin_token)
    expect("create staff", code, [200, 201], p)
    code, p = req("POST", "/identity/login", {"email": staff_email, "password": DEFAULT_PWD})
    expect("staff login", code, 200, p)
    staff_token = token_of(p)
    code, p = req("POST", "/identity/select-gym", {"gymId": gym_id}, token=staff_token)
    if code == 200:
        staff_token = token_of(p)
    creds["accounts"]["staff"] = {"email": staff_email, "password": DEFAULT_PWD, "token": staff_token}

    # 7. trainer via staff-create-trainer (needs staff JWT with gym selected)
    trainer_email = f"trainer_{TAG}@e2e.local"
    code, p = req("POST", "/identity/users/staff-create-trainer", {
        "firstName": "TrainerE2E", "lastName": TAG,
        "phone": "+201000000002", "email": trainer_email,
    }, token=staff_token)
    expect("create trainer", code, [200, 201], p)
    trainer_pwd = p["data"].get("initialPassword") if isinstance(p["data"], dict) else None
    trainer_pwd = trainer_pwd or DEFAULT_PWD
    code, p = req("POST", "/identity/login", {"email": trainer_email, "password": trainer_pwd})
    expect("trainer login", code, 200, p)
    trainer_token = token_of(p)
    code, p = req("POST", "/identity/select-gym", {"gymId": gym_id}, token=trainer_token)
    if code == 200:
        trainer_token = token_of(p)
    creds["accounts"]["trainer"] = {"email": trainer_email, "password": trainer_pwd, "token": trainer_token}

    # 7b. Upgrade gym to Pro plan so feature-gated flows work
    PRO_PLAN_ID = "00000000-0000-0000-0001-000000000003"
    code, p = req("PUT", f"/admin/gyms/{gym_id}/subscription/plan",
                  {"planId": PRO_PLAN_ID, "reason": "e2e cycle"}, token=admin_token)
    print(f"  upgrade-to-Pro: {code}")

    # 7c. Look up trainer user id, add to gym, approve so it lists
    code, p = req("GET", f"/admin/users?search={trainer_email}", token=admin_token)
    trainer_user_id = None
    if code == 200:
        items = p.get("data") or []
        if isinstance(items, dict):
            items = items.get("items") or []
        for u in items:
            if u.get("email") == trainer_email:
                trainer_user_id = u.get("id")
                break
    if trainer_user_id:
        code, _ = req("POST", f"/gyms/{gym_id}/trainers",
                      {"trainerId": trainer_user_id, "bio": "e2e", "specializations": ["strength"]},
                      token=owner_token)
        print(f"  add-trainer-to-gym: {code}")
        code, _ = req("POST", f"/gyms/{gym_id}/trainers/{trainer_user_id}/approve", {}, token=owner_token)
        print(f"  approve-trainer: {code}")
        creds["trainer_id"] = trainer_user_id

    # 8. trainee via staff-create
    trainee_email = f"trainee_{TAG}@e2e.local"
    code, p = req("POST", "/identity/users/staff-create", {
        "firstName": "TraineeE2E", "lastName": TAG,
        "phone": "+201000000003", "email": trainee_email, "section": "general",
    }, token=staff_token)
    expect("create trainee", code, [200, 201], p)
    trainee_pwd = p["data"].get("initialPassword") if isinstance(p["data"], dict) else None
    trainee_pwd = trainee_pwd or DEFAULT_PWD
    code, p = req("POST", "/identity/login", {"email": trainee_email, "password": trainee_pwd})
    expect("trainee login", code, 200, p)
    trainee_token = token_of(p)
    creds["accounts"]["trainee"] = {"email": trainee_email, "password": trainee_pwd, "token": trainee_token}

    out = OUT_DIR / f"creds_{TS}.json"
    out.write_text(json.dumps(creds, indent=2))
    latest = OUT_DIR / "creds_latest.json"
    latest.write_text(json.dumps(creds, indent=2))
    print(f"OK  wrote {out}")
    print(f"OK  wrote {latest}")


if __name__ == "__main__":
    main()
