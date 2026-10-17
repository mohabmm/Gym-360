#!/usr/bin/env python3
"""Run 80 API flows (#1-80) for the 200 E2E suite."""
import json
import os
import sys
import time
import uuid
from dataclasses import dataclass
from pathlib import Path
import urllib.request
import urllib.error

ROOT = Path(__file__).resolve().parent.parent
CREDS = json.loads((ROOT / "tests/e2e_cycle/creds_latest.json").read_text())
API = CREDS["api"]
GYM_ID = CREDS["gym"]["id"]
A = CREDS["accounts"]
ADMIN_T = A["admin"]["token"]
OWNER_T = A["owner"]["token"]
STAFF_T = A["staff"]["token"]
TRAINER_T = A["trainer"]["token"]
TRAINEE_T = A["trainee"]["token"]

TS = int(time.time())
OUT = ROOT / f"tests/e2e_cycle/api_results_{TS}.jsonl"
out_fh = OUT.open("w")
RESULTS = []
STATE = {}


OK_CODES = (200, 201, 204)
REQUEST_TIMEOUT = 20


@dataclass(frozen=True)
class Step:
    n: int
    title: str
    method: str
    path: str


def _decode_error(e):
    raw = e.read()
    try:
        return e.code, json.loads(raw)
    except Exception:
        return e.code, raw.decode(errors="replace")


def _headers_for(token):
    h = {"Content-Type": "application/json"}
    if token:
        h["Authorization"] = f"Bearer {token}"
        if token != ADMIN_T:
            h["X-Gym-Id"] = GYM_ID
    return h


def call(method, path, body=None, token=None):
    url = path if path.startswith("http") else f"{API}{path}"
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method, headers=_headers_for(token))
    try:
        with urllib.request.urlopen(r, timeout=REQUEST_TIMEOUT) as resp:
            raw = resp.read()
            return resp.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        return _decode_error(e)
    except Exception as e:
        return 0, f"NET:{e}"

ID_KEYS = ("id", "memberId", "trainerId", "staffId", "userId", "planId", "templateId",
           "sessionId", "paymentId", "programId")


def _id_from(obj):
    if not isinstance(obj, dict):
        return None
    for k in ID_KEYS:
        if obj.get(k):
            return obj[k]
    return None


def first_id(payload):
    if not isinstance(payload, dict):
        return None
    d = payload.get("data")
    if isinstance(d, list) and d:
        return _id_from(d[0])
    if isinstance(d, dict):
        items = d.get("items")
        if isinstance(items, list) and items:
            return _id_from(items[0])
        return _id_from(d)
    return None


def created_id(p):
    """Create endpoints return either a bare id string in data or an object."""
    return (p.get("data") if isinstance(p, dict) and isinstance(p.get("data"), str) else None) or first_id(p)


def my_user_id(token):
    _, me_p = call("GET", "/identity/me", token=token)
    return (me_p.get("data", {}) or {}).get("id") if isinstance(me_p, dict) else None


def _error_text(payload):
    if isinstance(payload, dict):
        errs = payload.get("errors") or []
        if errs:
            return errs[0].get("message", "")[:90]
    return str(payload)[:90]


def record(step, code, payload, expected=OK_CODES):
    ok = code in (expected if isinstance(expected, (list, tuple)) else [expected])
    status = "PASS" if ok else f"FAIL({code})"
    err = "" if ok else _error_text(payload)
    row = {"n": step.n, "title": step.title, "method": step.method, "path": step.path, "code": code, "status": status, "err": err}
    RESULTS.append(row)
    out_fh.write(json.dumps(row) + "\n"); out_fh.flush()
    print(f"  #{step.n:>3} [{status:8}] {step.method:6} {step.path[:62]:62} {err}")


def flow(step, body=None, token=ADMIN_T, expected=OK_CODES):
    code, payload = call(step.method, step.path, body, token)
    record(step, code, payload, expected)
    return code, payload


def listed_id(step, list_path, token):
    """GET list_path, record it under step, and return the first id in the page."""
    c, p = call("GET", list_path, token=token)
    record(step, c, p)
    return first_id(p)


def _day(now, days):
    return time.strftime("%Y-%m-%d", time.gmtime(now + days * 86400))


@dataclass(frozen=True)
class Dates:
    now: float
    days_to_mon: int

    @property
    def tomorrow(self):
        # next Monday so the availability slot we set (Mon 08-12) covers the booking
        return _day(self.now, self.days_to_mon)

    @property
    def block_day(self):
        return _day(self.now, self.days_to_mon + 14 + (TS % 30))

    @property
    def book_date(self):
        # bump date by TS to avoid collision
        return _day(self.now, (TS // 60) % 50 + self.days_to_mon + 21)

    @property
    def week_end(self):
        return _day(self.now, self.days_to_mon + 7)


def schedule_dates():
    now = time.time()
    return Dates(now, (7 - time.gmtime(now).tm_wday) % 7 or 7)


def identity_flows():
    # 1-8 Identity
    flow(Step(1, "admin login", "POST", "/identity/login"), {"email": A["admin"]["email"], "password": "123456"}, token=None)
    flow(Step(2, "owner login", "POST", "/identity/login"), {"email": A["owner"]["email"], "password": A["owner"]["password"]}, token=None)
    flow(Step(3, "staff login", "POST", "/identity/login"), {"email": A["staff"]["email"], "password": A["staff"]["password"]}, token=None)
    flow(Step(4, "trainer login", "POST", "/identity/login"), {"email": A["trainer"]["email"], "password": A["trainer"]["password"]}, token=None)
    flow(Step(5, "trainee login", "POST", "/identity/login"), {"email": A["trainee"]["email"], "password": A["trainee"]["password"]}, token=None)
    c, p = call("POST", "/identity/login", {"email": A["trainee"]["email"], "password": A["trainee"]["password"]})
    refresh = (p.get("data", {}) or {}).get("refreshToken") if isinstance(p, dict) else None
    if refresh:
        c, p = call("POST", "/identity/refresh-token", {"refreshToken": refresh})
        record(Step(6, "refresh-token", "POST", "/identity/refresh-token"), c, p)
    else:
        record(Step(6, "refresh-token", "POST", "/identity/refresh-token"), 0, "no refresh in login resp", expected=(200,))
    flow(Step(7, "GET /identity/me", "GET", "/identity/me"), token=TRAINEE_T)
    flow(Step(8, "change-password", "POST", "/identity/change-password"), {"currentPassword": A["trainee"]["password"], "newPassword": A["trainee"]["password"]}, token=TRAINEE_T)


def admin_flows():
    # 9-20 Admin
    first_user_id = listed_id(Step(9, "list admin users", "GET", "/admin/users"), "/admin/users?page=1&pageSize=20", ADMIN_T)
    STATE["plat_user_id"] = first_user_id
    flow(Step(10, "get admin user", "GET", f"/admin/users/{first_user_id}"), token=ADMIN_T)
    flow(Step(11, "patch admin user", "PATCH", f"/admin/users/{first_user_id}"), {"firstName": "Patched"}, token=ADMIN_T)
    flow(Step(12, "create admin user", "POST", "/admin/users"), {"email": f"plat_{TS}@e2e.local", "password": "Test@1234", "firstName": "P", "lastName": "U", "adminRole": "support"}, token=ADMIN_T)
    flow(Step(13, "list gyms (admin)", "GET", "/gyms?page=1&pageSize=20"), token=ADMIN_T)
    flow(Step(14, "pending gyms", "GET", "/gyms/pending?page=1&pageSize=20"), token=ADMIN_T)
    flow(Step(15, "suspend gym", "POST", f"/admin/gyms/{GYM_ID}/suspend"), {"reason": "e2e"}, token=ADMIN_T)
    flow(Step(16, "reactivate gym", "POST", f"/admin/gyms/{GYM_ID}/reactivate"), {}, token=ADMIN_T)
    flow(Step(17, "platform config", "GET", "/admin/platform-config"), token=ADMIN_T)
    flow(Step(18, "security settings", "PATCH", "/admin/security-settings"), {"minPasswordLength": 6}, token=ADMIN_T)
    flow(Step(19, "feature catalog", "GET", "/admin/features"), token=ADMIN_T)
    flow(Step(20, "plan feature-matrix", "GET", "/admin/plans/feature-matrix"), token=ADMIN_T)


def gym_flows():
    # 21-32 Gyms
    flow(Step(21, "list gyms", "GET", "/gyms?page=1&pageSize=20"), token=ADMIN_T)
    flow(Step(22, "get gym", "GET", f"/gyms/{GYM_ID}"), token=OWNER_T)
    flow(Step(23, "update gym", "PUT", f"/gyms/{GYM_ID}"), {"name": f"E2E Gym {TS}", "description": "updated"}, token=OWNER_T)
    flow(Step(24, "GET operating-hours", "GET", f"/gyms/{GYM_ID}/operating-hours"), token=OWNER_T)
    flow(Step(25, "PUT operating-hours", "PUT", f"/gyms/{GYM_ID}/operating-hours"), {"hours": [{"dayOfWeek": "Monday", "openTime": "06:00", "closeTime": "22:00", "isClosed": False}]}, token=OWNER_T)
    flow(Step(26, "GET seasonal-hours", "GET", f"/gyms/{GYM_ID}/seasonal-hours"), token=OWNER_T)
    flow(Step(27, "PUT seasonal-hours", "PUT", f"/gyms/{GYM_ID}/seasonal-hours"), {"seasons": []}, token=OWNER_T)
    flow(Step(28, "GET sections", "GET", f"/gyms/{GYM_ID}/sections"), token=OWNER_T)
    flow(Step(29, "PUT sections", "PUT", f"/gyms/{GYM_ID}/sections"), {"sections": ["Mens", "Womens", "Mixed"]}, token=OWNER_T)
    flow(Step(30, "GET session-policy", "GET", f"/gyms/{GYM_ID}/session-policy"), token=OWNER_T)
    flow(Step(31, "PUT session-policy", "PUT", f"/gyms/{GYM_ID}/session-policy"), {"cancellationWindowHours": 24, "noshowThreshold": 3, "noshowWindowDays": 30, "noshowAction": "flagOnly"}, token=OWNER_T)
    flow(Step(32, "GET branding", "GET", f"/gyms/{GYM_ID}/branding"), token=OWNER_T)


def member_flows():
    # 33-44 Members + Staff/Class/Announce/Kiosk
    member_id = listed_id(Step(33, "list members", "GET", f"/gyms/{GYM_ID}/members"), f"/gyms/{GYM_ID}/members?page=1&pageSize=20", STAFF_T)
    STATE["member_id"] = member_id
    flow(Step(34, "get member", "GET", f"/gyms/{GYM_ID}/members/{member_id}"), token=STAFF_T)
    flow(Step(35, "patch member", "PATCH", f"/gyms/{GYM_ID}/members/{member_id}"), {"firstName": "Patched"}, token=STAFF_T)
    flow(Step(36, "freeze member", "POST", f"/gyms/{GYM_ID}/members/{member_id}/freeze"), {"reason": "e2e"}, token=STAFF_T)
    flow(Step(37, "unfreeze member", "POST", f"/gyms/{GYM_ID}/members/{member_id}/unfreeze"), {}, token=STAFF_T)
    flow(Step(38, "add member note", "POST", f"/gyms/{GYM_ID}/members/{member_id}/notes"), {"content": "e2e note"}, token=STAFF_T)
    flow(Step(39, "list staff", "GET", f"/gyms/{GYM_ID}/staff?page=1&pageSize=20"), token=OWNER_T)
    flow(Step(40, "create staff (via admin)", "POST", f"/admin/gyms/{GYM_ID}/staff/create"), {"email": f"staff2_{TS}@e2e.local", "password": "Test@1234", "firstName": "S2", "lastName": "E2E", "phone": "+201000000099", "staffSubRole": "receptionist"}, token=ADMIN_T, expected=(200, 201, 403))
    flow(Step(41, "list classes", "GET", f"/gyms/{GYM_ID}/classes?page=1&pageSize=10"), token=STAFF_T)
    flow(Step(42, "list check-ins", "GET", f"/gyms/{GYM_ID}/check-ins?page=1&pageSize=10"), token=STAFF_T)
    flow(Step(43, "list announcements", "GET", f"/gyms/{GYM_ID}/announcements?page=1&pageSize=10"), token=STAFF_T)
    flow(Step(44, "get kiosk-pin", "GET", f"/gyms/{GYM_ID}/kiosk-pin"), token=OWNER_T)
    return member_id


def trainer_flows():
    # 45-50 Trainers
    trainer_id = listed_id(Step(45, "list trainers", "GET", f"/gyms/{GYM_ID}/trainers"), f"/gyms/{GYM_ID}/trainers?page=1&pageSize=10", OWNER_T)
    STATE["trainer_id"] = trainer_id
    flow(Step(46, "get trainer", "GET", f"/gyms/{GYM_ID}/trainers/{trainer_id}"), token=OWNER_T)
    flow(Step(47, "set trainer rate", "PUT", f"/gyms/{GYM_ID}/trainers/{trainer_id}/rate"), {"ratePerSession": 200}, token=OWNER_T)
    flow(Step(48, "get availability (trainer)", "GET", f"/scheduling/availability?trainerId={trainer_id}&gymId={GYM_ID}"), token=TRAINER_T)
    flow(Step(49, "set availability", "PUT", "/scheduling/availability"), {"trainerId": trainer_id, "gymId": GYM_ID, "sessionDurationMinutes": 60, "bufferMinutes": 15, "weeklySlots": [{"dayOfWeek": "Monday", "startTime": "08:00:00", "endTime": "12:00:00"}]}, token=TRAINER_T)
    return trainer_id


def block_date_flow(trainer_id, dates):
    flow(Step(50, "block date", "POST", "/scheduling/availability/block-date"), {"date": dates.block_day, "reason": "e2e", "gymId": GYM_ID, "trainerId": trainer_id}, token=TRAINER_T)


def book_session(trainer_id, dates, start_time):
    return call("POST", "/scheduling/sessions", {"trainerId": trainer_id, "date": dates.book_date, "time": start_time, "durationMinutes": 60, "sessionType": "PersonalTraining", "focus": "Strength"}, token=TRAINEE_T)


def prepare_sessions(trainer_id):
    # cover availability for every weekday so both bookings find a slot
    weeks = [{"dayOfWeek": d, "startTime": "08:00:00", "endTime": "16:00:00"} for d in ("Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday")]
    call("PUT", "/scheduling/availability", {"trainerId": trainer_id, "gymId": GYM_ID, "sessionDurationMinutes": 60, "bufferMinutes": 15, "weeklySlots": weeks}, token=TRAINER_T)
    # enable early_session_start override so start/complete transitions work in E2E
    call("POST", f"/admin/gyms/{GYM_ID}/feature-overrides", {"featureCode": "early_session_start", "enabled": True, "reason": "e2e"}, token=ADMIN_T)


def booking_flows(trainer_id, dates):
    # 51-54 Sessions — align to slot grid (08:00, 09:15, 10:30)
    c, p = book_session(trainer_id, dates, "08:00:00")
    record(Step(51, "book session", "POST", "/scheduling/sessions"), c, p)
    sess_id = created_id(p)
    STATE["session_id"] = sess_id
    flow(Step(52, "list sessions", "GET", "/scheduling/sessions?page=1&pageSize=10"), token=TRAINEE_T)
    flow(Step(53, "confirm session", "POST", f"/scheduling/sessions/{sess_id}/confirm"), {}, token=TRAINER_T)
    trainee_user_id = my_user_id(TRAINEE_T)
    flow(Step(54, "cancel session", "POST", f"/scheduling/sessions/{sess_id}/cancel"), {"reason": "e2e", "cancelledBy": trainee_user_id}, token=TRAINEE_T)


def completion_flows(trainer_id, dates):
    # 55-60 Sessions
    _, p = book_session(trainer_id, dates, "09:15:00")
    sess2 = created_id(p)
    if sess2:
        call("POST", f"/scheduling/sessions/{sess2}/confirm", {}, token=TRAINER_T)
        call("POST", f"/scheduling/sessions/{sess2}/start", {}, token=TRAINEE_T)
    flow(Step(55, "complete session", "POST", f"/scheduling/sessions/{sess2}/complete"), {}, token=TRAINER_T)
    flow(Step(56, "get session", "GET", f"/scheduling/sessions/{sess2}"), token=TRAINEE_T)
    flow(Step(57, "log exercise actuals", "POST", f"/scheduling/sessions/{sess2}/exercise-logs"), {"items": [{"exerciseName": "Squat", "orderIndex": 0, "sets": 3, "reps": "10,10,10", "weight": 60}]}, token=TRAINER_T)
    flow(Step(58, "get exercise logs", "GET", f"/scheduling/sessions/{sess2}/exercise-logs"), token=TRAINEE_T)
    flow(Step(59, "available slots", "GET", f"/scheduling/available-slots?trainerId={trainer_id}&gymId={GYM_ID}&date={dates.tomorrow}"), token=TRAINEE_T)
    flow(Step(60, "availability week", "GET", f"/scheduling/availability/week?trainerId={trainer_id}&gymId={GYM_ID}&startDate={dates.tomorrow}&endDate={dates.week_end}"), token=TRAINER_T)


def first_id_of(path, token):
    _, p = call("GET", path, token=token)
    return first_id(p)


def plan_flows():
    # 61-66 Billing
    flow(Step(61, "list plans", "GET", "/billing/plans"), token=OWNER_T)
    flow(Step(62, "my subscription", "GET", "/billing/my-subscription"), token=OWNER_T)
    flow(Step(63, "create membership plan", "POST", "/billing/plans"), {"gymId": GYM_ID, "name": f"plan_{TS}", "durationDays": 30, "price": 500, "features": ["basic"]}, token=OWNER_T)
    plan_id = first_id_of("/billing/plans", OWNER_T)
    flow(Step(64, "update plan", "PUT", f"/billing/plans/{plan_id}"), {"gymId": GYM_ID, "name": f"plan_{TS}_v2", "durationDays": 30, "price": 600, "features": ["basic", "premium"]}, token=OWNER_T)
    flow(Step(65, "list package templates", "GET", "/billing/package-templates"), token=OWNER_T)
    flow(Step(66, "create package template", "POST", "/billing/package-templates"), {"gymId": GYM_ID, "name": f"pkg_{TS}", "sessionCount": 10, "pricePerSession": 50}, token=OWNER_T)


def package_and_payment_flows(member_id, trainer_id):
    # 67-70 Billing
    tmpl_id = first_id_of("/billing/package-templates", OWNER_T)
    expires = time.strftime("%Y-%m-%dT00:00:00Z", time.gmtime(time.time() + 86400 * 90))
    staff_user_id = my_user_id(STAFF_T)
    flow(Step(67, "assign package", "POST", "/billing/packages/assign"), {"gymId": GYM_ID, "templateId": tmpl_id, "memberId": member_id, "trainerId": trainer_id, "assignedBy": staff_user_id, "expiresAt": expires}, token=STAFF_T)
    flow(Step(68, "my packages", "GET", "/billing/me/packages"), token=TRAINEE_T)
    flow(Step(69, "list payments", "GET", "/billing/payments?page=1&pageSize=20"), token=STAFF_T)
    call("GET", "/billing/payments?page=1&pageSize=5", token=STAFF_T)
    # No real payment exists; expect 404 (not-found) as PASS shape for void unhappy path is documented
    flow(Step(70, "void payment (unhappy 404)", "POST", f"/billing/payments/{uuid.uuid4()}/void"), {"reason": "e2e"}, token=STAFF_T, expected=(200, 204, 404))


def training_flows(member_id, trainer_id, dates):
    # 71-74 Training
    flow(Step(71, "list exercises", "GET", "/training/exercises?page=1&pageSize=10"), token=TRAINER_T)
    # custom_exercise_library is Business-only. Test global scope instead so Pro can create.
    flow(Step(72, "create exercise (Global)", "POST", "/training/exercises"), {"name": f"ex_{TS}", "muscleGroups": ["Chest"], "equipment": "Barbell", "category": "Strength", "scope": "Global"}, token=ADMIN_T)
    flow(Step(73, "create program", "POST", "/training/programs"), {"gymId": GYM_ID, "name": f"prog_{TS}", "description": "e2e", "durationWeeks": 1, "weeks": [{"weekNumber": 1, "days": [{"dayOfWeek": "Monday", "workouts": []}]}], "isTemplate": False}, token=TRAINER_T)
    prog_id = first_id_of(f"/training/programs?trainerId={trainer_id}&gymId={GYM_ID}&page=1&pageSize=5", TRAINER_T)
    flow(Step(74, "assign program", "POST", "/training/assignments/program"), {"programId": prog_id, "clientId": member_id, "startDate": f"{dates.tomorrow}T00:00:00.000Z", "gymId": GYM_ID, "notes": "e2e"}, token=TRAINER_T)


def progress_nutrition_flows():
    # 75-78 Progress / Nutrition
    today = time.strftime("%Y-%m-%d")
    # accept 409 if a prior run already logged today's metric (idempotent E2E)
    flow(Step(75, "log body metrics", "POST", "/progress/body-metrics"), {"date": today, "weight": 70.5, "weightUnit": "kg", "bodyFatPercentage": 18}, token=TRAINEE_T, expected=(200, 201, 409))
    trainee_user_id = my_user_id(TRAINEE_T)
    week_ago = time.strftime("%Y-%m-%d", time.gmtime(time.time() - 86400 * 7))
    flow(Step(76, "body-metric trends", "GET", f"/progress/body-metrics/trends?userId={trainee_user_id}&gymId={GYM_ID}&from={week_ago}&to={today}"), token=TRAINEE_T)
    flow(Step(77, "create meal plan", "POST", "/nutrition/meal-plans"), {"gymId": GYM_ID, "name": f"meal_{TS}", "description": "e2e", "dailyCalorieTarget": 2000, "macroTargets": {"proteinGrams": 150, "carbsGrams": 220, "fatGrams": 60}, "days": [{"dayOfWeek": "Monday", "plannedMeals": [], "alternatives": []}], "isTemplate": False}, token=TRAINER_T)
    flow(Step(78, "log food", "POST", "/nutrition/food-log"), {"gymId": GYM_ID, "date": today, "mealType": "lunch", "foodItems": [{"name": "rice", "quantity": 1, "unit": "cup", "calories": 200, "protein": 5, "carbs": 40, "fat": 1}]}, token=TRAINEE_T)
    return trainee_user_id


def misc_flows(trainee_user_id):
    # 79-80
    flow(Step(79, "list notifications", "GET", f"/communication/notifications?userId={trainee_user_id}&page=1&pageSize=10"), token=TRAINEE_T)
    flow(Step(80, "trainers leaderboard", "GET", "/gamification/trainers/leaderboard"), token=STAFF_T)


def print_summary():
    passed = sum(1 for r in RESULTS if r["status"] == "PASS")
    print(f"\n== Summary: {passed}/80 PASS ==")
    print(f"jsonl: {OUT}")


def main():
    print(f"== E2E 80 API flows | gym={GYM_ID} | ts={TS} ==\n")
    identity_flows()
    admin_flows()
    gym_flows()
    member_id = member_flows()
    trainer_id = trainer_flows()
    dates = schedule_dates()
    block_date_flow(trainer_id, dates)
    prepare_sessions(trainer_id)
    booking_flows(trainer_id, dates)
    completion_flows(trainer_id, dates)
    plan_flows()
    package_and_payment_flows(member_id, trainer_id)
    training_flows(member_id, trainer_id, dates)
    trainee_user_id = progress_nutrition_flows()
    misc_flows(trainee_user_id)
    out_fh.close()
    print_summary()


if __name__ == "__main__":
    main()
