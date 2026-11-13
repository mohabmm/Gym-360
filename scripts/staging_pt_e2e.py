#!/usr/bin/env python3
"""Staging E2E for the PT module (branch Validation_1_PrivateSessions).

Provisions a fresh, tagged pilot gym (owner, manager/desk, coach, trainee) through the
real API, then walks the PT journey and the feature-flag checks:

  desk assigns a free trial with a coach -> coach one-tap logs + records measurements ->
  trainee confirms and sees progress -> last-trial-session alert (coach + desk) ->
  "not converted" list -> trainee buys a package -> 3-left / 1-left reminders ->
  owner PT dashboard numbers -> dispute + desk reverse.

  Flags: OFF modules return 403 FEATURE_DISABLED:<key>; Super Admin API is super_admin
  only; one override turns on one feature for this gym only (other gyms unchanged) and is
  audited; preset business turns everything on; back to pt_module.

The coach's 5-minute double-tap guard is simulated by back-dating this test gym's own
quick-log rows (SQL on the test gym's g{N}_scheduling schema only).

Usage:  API_BASE=http://127.0.0.1:8090/api/v1 python3 scripts/staging_pt_e2e.py
"""
import json
import os
import subprocess
import sys
import time
import urllib.parse
from datetime import datetime, timedelta, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import provision_e2e_cycle as prov  # noqa: E402  (reuses req/login/provisioning helpers)

req, login, DEFAULT_PWD = prov.req, prov.login, prov.DEFAULT_PWD
DB_CONTAINER = os.environ.get("DB_CONTAINER", "gymapp-db")
ENV_FILE = os.environ.get("STAGING_ENV_FILE", "/etc/dokploy/compose/gym-360-staging-ogwxss/code/.env")
EVENT_TIMEOUT = 90

results = []


def check(name, ok, detail=""):
    results.append((name, bool(ok), detail))
    print(f"[{'PASS' if ok else 'FAIL'}] {name}" + (f" — {detail}" if detail and not ok else ""))
    return ok


def data(code, payload):
    return payload.get("data") if isinstance(payload, dict) else None


def get(path, token, gym_id=None):
    return req_h("GET", path, None, (token, gym_id))


def req_h(method, path, body, auth):
    """prov.req plus the X-Gym-Id header. `auth` is a token or a (token, gym_id) pair."""
    import urllib.request
    import urllib.error
    token, gym_id = auth if isinstance(auth, tuple) else (auth, None)
    url = f"{prov.API}{path}"
    headers = {"Content-Type": "application/json", "Authorization": f"Bearer {token}"}
    if gym_id:
        headers["X-Gym-Id"] = gym_id
    r = urllib.request.Request(url, data=json.dumps(body).encode() if body is not None else None,
                               method=method, headers=headers)
    try:
        with urllib.request.urlopen(r, timeout=20) as resp:
            raw = resp.read()
            return resp.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        return prov._decode_error(e)


def first_code(payload):
    try:
        return payload["errors"][0]["code"]
    except Exception:
        return None


def eventually(fn, timeout=EVENT_TIMEOUT):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if fn():
            return True
        time.sleep(3)
    return fn()


def items_of(d):
    if isinstance(d, dict):
        return d.get("items") or []
    return d or []


def phone(n):
    """Unique Egyptian-format phone per run (the shared helper's fixed numbers are taken)."""
    return f"+2010{(prov.TS * 10 + n) % 10**8:08d}"


def create_staff(creds, admin_token, gym_id):
    email = f"staff_{prov.TAG}@e2e.local"
    code, p = req("POST", f"/admin/gyms/{gym_id}/staff/create", {
        "email": email, "password": DEFAULT_PWD, "firstName": "DeskE2E", "lastName": prov.TAG,
        "phone": phone(1), "staffSubRole": "manager"}, token=admin_token)
    prov.expect("create staff (desk)", code, [200, 201], p)
    token = prov.try_select_gym(login("staff login", email, DEFAULT_PWD), gym_id)
    creds["accounts"]["staff"] = {"email": email, "password": DEFAULT_PWD, "token": token}
    return token


def create_trainer(creds, staff_token, gym_id):
    email = f"trainer_{prov.TAG}@e2e.local"
    code, p = req("POST", "/identity/users/staff-create-trainer", {
        "firstName": "CoachE2E", "lastName": prov.TAG, "phone": phone(2), "email": email}, token=staff_token)
    prov.expect("create trainer (coach)", code, [200, 201], p)
    pwd = prov.initial_password(p)
    creds["accounts"]["trainer"] = {"email": email, "password": pwd,
                                    "token": prov.try_select_gym(login("trainer login", email, pwd), gym_id)}
    return email


def create_trainee(creds, staff_token):
    email = f"trainee_{prov.TAG}@e2e.local"
    code, p = req("POST", "/identity/users/staff-create", {
        "firstName": "TraineeE2E", "lastName": prov.TAG, "phone": phone(3), "email": email,
        "section": "general"}, token=staff_token)
    prov.expect("create trainee", code, [200, 201], p)
    pwd = prov.initial_password(p)
    creds["accounts"]["trainee"] = {"email": email, "password": pwd, "token": login("trainee login", email, pwd)}


# ---------------------------------------------------------------- provisioning

def provision():
    creds = {"accounts": {}}
    admin = prov.admin_login(creds)
    owner_email = f"owner_{prov.TAG}@e2e.local"
    owner_id = prov.create_owner(admin, owner_email)
    gym_id = prov.register_gym(creds, admin, owner_id)
    owner = prov.owner_session(creds, owner_email, owner_id, gym_id)
    desk = create_staff(creds, admin, gym_id)
    trainer_email = create_trainer(creds, desk, gym_id)
    coach_id = prov.find_user_id(admin, trainer_email)
    prov.attach_trainer(creds, owner, gym_id, coach_id)
    create_trainee(creds, desk)
    trainee_email = creds["accounts"]["trainee"]["email"]
    trainee_id = prov.find_user_id(admin, trainee_email)
    coach = prov.try_select_gym(login("coach re-login", trainer_email, creds["accounts"]["trainer"]["password"]), gym_id)
    trainee = prov.try_select_gym(creds["accounts"]["trainee"]["token"], gym_id)
    desk_id = prov.find_user_id(admin, creds["accounts"]["staff"]["email"])
    return dict(admin=admin, owner=owner, desk=desk, coach=coach, trainee=trainee, gym=gym_id,
                coach_id=coach_id, trainee_id=trainee_id, desk_id=desk_id, creds=creds)


# ---------------------------------------------------------------- SQL helper (test gym only)

def sql(query):
    env = dict(line.split("=", 1) for line in Path(ENV_FILE).read_text().splitlines() if "=" in line)
    out = subprocess.run(
        ["docker", "exec", DB_CONTAINER, "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa",
         "-P", env["DB_SA_PASSWORD"].strip(), "-C", "-d", "GymApp", "-h", "-1", "-W", "-Q",
         "SET NOCOUNT ON; " + query],
        capture_output=True, text=True, timeout=60)
    if out.returncode != 0:
        raise RuntimeError(out.stderr or out.stdout)
    return out.stdout.strip()


def schema_prefix(gym_id):
    return sql(f"SELECT SchemaPrefix FROM gyms_registry.gyms WHERE GymId = '{gym_id}'").splitlines()[0].strip()


def age_quick_logs(s):
    sql(f"UPDATE [{s['prefix']}_scheduling].[sessions] SET CreatedAt = DATEADD(minute, -10, CreatedAt) "
        f"WHERE TrainerId = '{s['coach_id']}' AND MemberId = '{s['trainee_id']}'")


# ---------------------------------------------------------------- journey

def quick_assign(s, sessions, is_trial, price=None):
    body = {"gymId": s["gym"], "trainerId": s["coach_id"], "memberId": s["trainee_id"],
            "sessionCount": sessions, "assignedBy": s["desk_id"], "isTrial": is_trial}
    if price:
        body.update(totalPrice=price, paymentMethod="Cash")
    return req_h("POST", "/billing/packages/quick-assign", body, (s["desk"], s["gym"]))


def quick_log(s):
    code, p = req_h("POST", "/scheduling/sessions/quick-log", {"memberId": s["trainee_id"]}, (s["coach"], s["gym"]))
    return code, data(code, p) or {}, p


def has_notification(token, gym, kind):
    code, p = get("/communication/notifications?page=1&pageSize=100", token, gym)
    return code == 200 and any((n.get("data") or {}).get("type") == kind for n in items_of(data(code, p)))


def not_converted(s):
    code, p = get("/billing/trials/not-converted", (s["desk"], s["gym"]))
    return {t["memberId"] for t in (data(code, p) or [])} if code == 200 else set()


def remaining(s):
    code, p = get("/billing/me/packages", (s["trainee"], s["gym"]))
    return sum(x.get("remainingSessions", 0) for x in items_of(data(code, p)) if x.get("status") == "Active")


def coach_row(s):
    now = datetime.now(timezone.utc)
    q = urllib.parse.urlencode({"from": (now - timedelta(days=1)).isoformat(), "to": (now + timedelta(days=1)).isoformat()})
    code, p = get(f"/billing/pt-dashboard?{q}", (s["owner"], s["gym"]))
    rows = (data(code, p) or {}).get("coaches") or []
    return next((r for r in rows if r.get("trainerId") == s["coach_id"]), None)


def expect_reminder(token_key, s, kind, label):
    check(f"{label} reaches the {token_key}", eventually(lambda: has_notification(s[token_key], s["gym"], kind)))


def journey_trial(s):
    """Free trial: assign, one-tap log, measurements, trainee confirms, last-session alert."""
    code, _ = quick_assign(s, 2, True)
    check("desk assigns a free 2-session trial with a coach", code == 201, code)
    code, _ = quick_assign(s, 1, True)
    check("second trial for the same trainee is refused (409)", code == 409, code)

    code, first, raw = quick_log(s)
    check("coach one-tap logs session 1 (1 left)", code == 200 and first.get("remainingSessions") == 1, raw)
    code, again, _ = quick_log(s)
    check("double tap within 5 min does not log twice", (code, again.get("alreadyLogged")) == (200, True), again)

    code, p = req_h("POST", "/progress/body-metrics/client", {
        "clientId": s["trainee_id"], "gymId": s["gym"], "date": datetime.now(timezone.utc).strftime("%Y-%m-%d"),
        "weight": 92.5, "weightUnit": "kg", "bodyFatPercentage": 24}, (s["coach"], s["gym"]))
    check("coach records measurements from session 1", code == 201, p)

    sid = first.get("sessionId")
    code, p = get(f"/scheduling/sessions?memberId={s['trainee_id']}&memberConfirmation=Pending&page=1&pageSize=20", s["trainee"], s["gym"])
    check("trainee sees the session waiting for confirmation", sid in [x.get("id") for x in items_of(data(code, p))], p)
    code, p = req_h("POST", f"/scheduling/sessions/{sid}/member-confirm", None, (s["trainee"], s["gym"]))
    check("trainee confirms the session", code == 204, p)
    code, p = get(f"/progress/body-metrics?gymId={s['gym']}", s["trainee"], s["gym"])
    weights = [m.get("weight") for m in (data(code, p) or [])]
    check("trainee sees progress (92.5 kg)", 92.5 in weights, p)

    expect_reminder("coach", s, "trial_last_session", "last-trial-session alert")
    expect_reminder("desk", s, "trial_last_session", "last-trial-session alert")


def journey_conversion(s):
    """Trial finishes, shows as not converted, trainee buys a package, 3-left / 1-left reminders."""
    age_quick_logs(s)
    code, d, raw = quick_log(s)
    check("coach logs session 2 (trial finished, 0 left)", code == 200 and d.get("remainingSessions") == 0, raw)
    check("trial appears in the desk's 'not converted' list",
          eventually(lambda: s["trainee_id"] in not_converted(s)))

    code, p = quick_assign(s, 4, False, 2000)
    check("trainee buys a 4-session package at the desk", code == 201, p)
    check("trainee leaves the 'not converted' list", s["trainee_id"] not in not_converted(s))
    check("remaining sessions counter shows 4", eventually(lambda: remaining(s) == 4, 30), remaining(s))

    for _ in range(3):
        age_quick_logs(s)
        quick_log(s)
    check("remaining sessions counter shows 1 after 3 logs", eventually(lambda: remaining(s) == 1, 30), remaining(s))
    expect_reminder("trainee", s, "renewal_3_left", "3-left reminder")
    expect_reminder("trainee", s, "renewal_1_left", "1-left reminder")
    check("1-left reminder reaches the coach", has_notification(s["coach"], s["gym"], "renewal_1_left"))


def journey_dashboard(s):
    """Owner PT dashboard numbers after the trial and the paid package."""
    check("owner dashboard counts all 5 sessions",
          eventually(lambda: (coach_row(s) or {}).get("sessionsLogged") == 5))
    row = coach_row(s) or {}
    trials = (row.get("trialsFinished"), row.get("trialsConverted"), row.get("conversionRate"))
    check("owner dashboard: 1 trial finished, 1 converted, 100%", trials == (1, 1, 100), row)
    packages = (row.get("newPaidPackages"), row.get("renewals"))
    check("owner dashboard: 1 new paid package, 0 renewals", packages == (1, 0), row)


def journey_dispute(s):
    """Dispute -> desk reverse gives the session back."""
    age_quick_logs(s)
    _, d, _ = quick_log(s)
    sid = d.get("sessionId")
    eventually(lambda: remaining(s) == 0, 30)
    code, p = req_h("POST", f"/scheduling/sessions/{sid}/member-dispute", {"reason": "Not me"}, (s["trainee"], s["gym"]))
    check("trainee disputes a session", code == 204, p)
    code, p = get("/scheduling/sessions/review", s["desk"], s["gym"])
    check("desk sees the disputed session", sid in [x.get("id") for x in (data(code, p) or [])], p)
    code, p = req_h("POST", f"/scheduling/sessions/{sid}/reverse", None, (s["desk"], s["gym"]))
    check("desk reverses it", code == 204, p)
    check("session is given back to the package", eventually(lambda: remaining(s) == 1, 30), remaining(s))


def journey(s):
    journey_trial(s)
    journey_conversion(s)
    journey_dashboard(s)
    journey_dispute(s)


# ---------------------------------------------------------------- flags

def my_flags(token, gym):
    code, p = get("/feature-flags/me", token, gym)
    return data(code, p) or {}


def enabled_flags(token, gym):
    return set(my_flags(token, gym).get("enabled") or [])


PT_FEATURES = {"pt.trial_sessions", "pt.progress_measurements", "pt.trial_end_alerts", "pt.packages",
               "pt.dual_session_logging", "pt.renewal_reminders", "pt.owner_dashboard"}


def blocked_calls(s):
    """One call per OFF feature: flag key -> (method, path, token)."""
    gym = s["gym"]
    return {
        "classes": ("GET", f"/gyms/{gym}/classes?page=1&pageSize=5", s["desk"]),
        "gym.attendance": ("GET", f"/gyms/{gym}/check-ins?page=1&pageSize=5", s["desk"]),
        "gym.finance": ("GET", "/billing/revenue", s["owner"]),
        "communication.announcements": ("GET", f"/gyms/{gym}/announcements?page=1&pageSize=5", s["desk"]),
        "nutrition": ("GET", "/nutrition/meal-plans", s["trainee"]),
        "coach.leaderboard": ("GET", "/gamification/trainers/leaderboard?page=1&pageSize=5", s["desk"]),
        "training.workouts": ("GET", "/training/workouts?page=1&pageSize=5", s["coach"]),
        "analytics": ("GET", f"/gyms/{gym}/analytics", s["owner"]),
    }


def set_preset(s, preset):
    return req_h("PUT", f"/admin/feature-flags/gyms/{s['gym']}/preset", {"presetCode": preset}, s["admin"])


def flags_pt_module(s):
    """pt_module turns on exactly the PT features; everything else answers FEATURE_DISABLED."""
    gym = s["gym"]
    code, p = set_preset(s, "pt_module")
    check("super admin sets the pilot gym to pt_module", code == 200, p)
    enabled = enabled_flags(s["trainee"], gym)
    check("/feature-flags/me = exactly the 7 PT features", enabled == PT_FEATURES, sorted(enabled))

    for key, (m, path, tok) in blocked_calls(s).items():
        code, p = req_h(m, path, None, (tok, gym))
        got = (code, first_code(p))
        check(f"OFF feature {key} → 403 FEATURE_DISABLED:{key}", got == (403, f"FEATURE_DISABLED:{key}"), f"{code} {first_code(p)}")

    for role in ("owner", "desk", "coach", "trainee"):
        code, _ = get("/admin/feature-flags/gyms", s[role])
        check(f"Super Admin flag API refuses {role} (403)", code == 403, code)


def other_gyms(s):
    """Preset and override count of every gym except the pilot gym."""
    code, p = get("/admin/feature-flags/gyms?page=1&pageSize=200", s["admin"])
    return {g["gymId"]: (g["presetCode"], g["overrideCount"]) for g in items_of(data(code, p)) if g["gymId"] != s["gym"]}


def is_override_audit(a):
    values = (str(a.get("oldValue", "")), str(a.get("newValue", "")))
    return (a.get("changeType"), a.get("featureKey")) == ("override_set", "classes") \
        and values[0].startswith("off") and values[1].startswith("on") and bool(a.get("actorEmail"))


def flags_override(s):
    """One override turns classes on for the pilot gym only, is audited, and can be reset."""
    admin, gym = s["admin"], s["gym"]
    before = other_gyms(s)

    code, p = req_h("PUT", f"/admin/feature-flags/gyms/{gym}/overrides/classes", {"enabled": True, "reason": "staging e2e"}, admin)
    check("super admin turns classes ON for the pilot gym", code in (200, 204), code)
    check("pilot gym's apps see classes after a refresh", "classes" in enabled_flags(s["trainee"], gym))
    code, p = req_h("GET", f"/gyms/{gym}/classes?page=1&pageSize=5", None, (s["desk"], gym))
    check("classes API now answers for the pilot gym", code == 200, f"{code} {first_code(p)}")
    after = other_gyms(s)
    check("other gyms are unchanged", before == after, f"{before} vs {after}")
    code, p = get(f"/admin/feature-flags/audit?gymId={gym}&featureKey=classes&page=1&pageSize=10", admin)
    audit = items_of(data(code, p))
    check("audit log records the override (old → new)", any(is_override_audit(a) for a in audit), audit[:1])

    code, _ = req_h("DELETE", f"/admin/feature-flags/gyms/{gym}/overrides/classes", None, admin)
    reset = code in (200, 204) and "classes" not in enabled_flags(s["trainee"], gym)
    check("reset to preset removes the override", reset, code)


def flags_business(s):
    """Preset business turns everything on; then back to pt_module."""
    gym = s["gym"]
    code, _ = set_preset(s, "business")
    biz = enabled_flags(s["trainee"], gym)
    code_c, _ = get(f"/gyms/{gym}/classes?page=1&pageSize=5", s["desk"], gym)
    all_on = {"classes", "gym.finance", "nutrition", "analytics"} <= biz
    check("preset business shows all features", (code, code_c, all_on) == (200, 200, True), f"{len(biz)} enabled")
    code, _ = set_preset(s, "pt_module")
    check("back to pt_module", code == 200 and "classes" not in enabled_flags(s["trainee"], gym))


def flags(s):
    flags_pt_module(s)
    flags_override(s)
    flags_business(s)


def main():
    print(f"API = {prov.API}  tag = {prov.TAG}")
    s = provision()
    s["prefix"] = schema_prefix(s["gym"])
    print(f"gym={s['gym']} schema={s['prefix']} coach={s['coach_id']} trainee={s['trainee_id']}")
    flags_first = os.environ.get("FLAGS_ONLY") == "1"
    if not flags_first:
        journey(s)
    flags(s)
    out = Path(prov.OUT_DIR) / f"staging_pt_e2e_{prov.TS}.json"
    out.write_text(json.dumps({"tag": prov.TAG, "gym": s["gym"],
                               "accounts": {k: {kk: vv for kk, vv in v.items() if kk != "token"}
                                            for k, v in s["creds"]["accounts"].items()},
                               "results": [{"check": n, "ok": ok} for n, ok, _ in results]}, indent=2))
    failed = [n for n, ok, _ in results if not ok]
    print(f"\n{len(results) - len(failed)}/{len(results)} checks passed. Report: {out}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
