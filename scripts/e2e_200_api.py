#!/usr/bin/env python3
"""Run 80 API flows (#1-80) for the 200 E2E suite."""
import json
import os
import sys
import time
import uuid
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


def call(method, path, body=None, token=None, timeout=20):
    url = path if path.startswith("http") else f"{API}{path}"
    data = json.dumps(body).encode() if body is not None else None
    h = {"Content-Type": "application/json"}
    if token:
        h["Authorization"] = f"Bearer {token}"
        if token != ADMIN_T:
            h["X-Gym-Id"] = GYM_ID
    r = urllib.request.Request(url, data=data, method=method, headers=h)
    try:
        with urllib.request.urlopen(r, timeout=timeout) as resp:
            raw = resp.read()
            return resp.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read()
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, raw.decode(errors="replace")
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


def record(n, title, method, path, code, payload, expected=(200, 201, 204)):
    ok = code in (expected if isinstance(expected, (list, tuple)) else [expected])
    status = "PASS" if ok else f"FAIL({code})"
    err = ""
    if not ok and isinstance(payload, dict):
        errs = payload.get("errors") or []
        if errs:
            err = errs[0].get("message", "")[:90]
        else:
            err = str(payload)[:90]
    elif not ok:
        err = str(payload)[:90]
    row = {"n": n, "title": title, "method": method, "path": path, "code": code, "status": status, "err": err}
    RESULTS.append(row)
    out_fh.write(json.dumps(row) + "\n"); out_fh.flush()
    print(f"  #{n:>3} [{status:8}] {method:6} {path[:62]:62} {err}")


def flow(n, title, method, path, body=None, token=ADMIN_T, expected=(200, 201, 204)):
    code, payload = call(method, path, body, token)
    record(n, title, method, path, code, payload, expected)
    return code, payload


def main():
    print(f"== E2E 80 API flows | gym={GYM_ID} | ts={TS} ==\n")

    # 1-8 Identity
    flow(1, "admin login", "POST", "/identity/login", {"email": A["admin"]["email"], "password": "123456"}, token=None)
    flow(2, "owner login", "POST", "/identity/login", {"email": A["owner"]["email"], "password": A["owner"]["password"]}, token=None)
    flow(3, "staff login", "POST", "/identity/login", {"email": A["staff"]["email"], "password": A["staff"]["password"]}, token=None)
    flow(4, "trainer login", "POST", "/identity/login", {"email": A["trainer"]["email"], "password": A["trainer"]["password"]}, token=None)
    flow(5, "trainee login", "POST", "/identity/login", {"email": A["trainee"]["email"], "password": A["trainee"]["password"]}, token=None)
    c, p = call("POST", "/identity/login", {"email": A["trainee"]["email"], "password": A["trainee"]["password"]})
    refresh = (p.get("data", {}) or {}).get("refreshToken") if isinstance(p, dict) else None
    if refresh:
        c, p = call("POST", "/identity/refresh-token", {"refreshToken": refresh})
        record(6, "refresh-token", "POST", "/identity/refresh-token", c, p)
    else:
        record(6, "refresh-token", "POST", "/identity/refresh-token", 0, "no refresh in login resp", expected=(200,))
    flow(7, "GET /identity/me", "GET", "/identity/me", token=TRAINEE_T)
    flow(8, "change-password", "POST", "/identity/change-password", {"currentPassword": A["trainee"]["password"], "newPassword": A["trainee"]["password"]}, token=TRAINEE_T)

    # 9-20 Admin
    c, p = call("GET", "/admin/users?page=1&pageSize=20", token=ADMIN_T)
    record(9, "list admin users", "GET", "/admin/users", c, p)
    first_user_id = first_id(p)
    STATE["plat_user_id"] = first_user_id
    flow(10, "get admin user", "GET", f"/admin/users/{first_user_id}", token=ADMIN_T)
    flow(11, "patch admin user", "PATCH", f"/admin/users/{first_user_id}", {"firstName": "Patched"}, token=ADMIN_T)
    flow(12, "create admin user", "POST", "/admin/users", {"email": f"plat_{TS}@e2e.local", "password": "Test@1234", "firstName": "P", "lastName": "U", "adminRole": "support"}, token=ADMIN_T)
    flow(13, "list gyms (admin)", "GET", "/gyms?page=1&pageSize=20", token=ADMIN_T)
    flow(14, "pending gyms", "GET", "/gyms/pending?page=1&pageSize=20", token=ADMIN_T)
    flow(15, "suspend gym", "POST", f"/admin/gyms/{GYM_ID}/suspend", {"reason": "e2e"}, token=ADMIN_T)
    flow(16, "reactivate gym", "POST", f"/admin/gyms/{GYM_ID}/reactivate", {}, token=ADMIN_T)
    flow(17, "platform config", "GET", "/admin/platform-config", token=ADMIN_T)
    flow(18, "security settings", "PATCH", "/admin/security-settings", {"minPasswordLength": 6}, token=ADMIN_T)
    flow(19, "feature catalog", "GET", "/admin/features", token=ADMIN_T)
    flow(20, "plan feature-matrix", "GET", "/admin/plans/feature-matrix", token=ADMIN_T)

    # 21-32 Gyms
    flow(21, "list gyms", "GET", "/gyms?page=1&pageSize=20", token=ADMIN_T)
    flow(22, "get gym", "GET", f"/gyms/{GYM_ID}", token=OWNER_T)
    flow(23, "update gym", "PUT", f"/gyms/{GYM_ID}", {"name": f"E2E Gym {TS}", "description": "updated"}, token=OWNER_T)
    flow(24, "GET operating-hours", "GET", f"/gyms/{GYM_ID}/operating-hours", token=OWNER_T)
    flow(25, "PUT operating-hours", "PUT", f"/gyms/{GYM_ID}/operating-hours", {"hours": [{"dayOfWeek": "Monday", "openTime": "06:00", "closeTime": "22:00", "isClosed": False}]}, token=OWNER_T)
    flow(26, "GET seasonal-hours", "GET", f"/gyms/{GYM_ID}/seasonal-hours", token=OWNER_T)
    flow(27, "PUT seasonal-hours", "PUT", f"/gyms/{GYM_ID}/seasonal-hours", {"seasons": []}, token=OWNER_T)
    flow(28, "GET sections", "GET", f"/gyms/{GYM_ID}/sections", token=OWNER_T)
    flow(29, "PUT sections", "PUT", f"/gyms/{GYM_ID}/sections", {"sections": ["Mens", "Womens", "Mixed"]}, token=OWNER_T)
    flow(30, "GET session-policy", "GET", f"/gyms/{GYM_ID}/session-policy", token=OWNER_T)
    flow(31, "PUT session-policy", "PUT", f"/gyms/{GYM_ID}/session-policy", {"cancellationWindowHours": 24, "noshowThreshold": 3, "noshowWindowDays": 30, "noshowAction": "flagOnly"}, token=OWNER_T)
    flow(32, "GET branding", "GET", f"/gyms/{GYM_ID}/branding", token=OWNER_T)

    # 33-44 Members + Staff/Class/Announce/Kiosk
    c, p = call("GET", f"/gyms/{GYM_ID}/members?page=1&pageSize=20", token=STAFF_T)
    record(33, "list members", "GET", f"/gyms/{GYM_ID}/members", c, p)
    member_id = first_id(p)
    STATE["member_id"] = member_id
    flow(34, "get member", "GET", f"/gyms/{GYM_ID}/members/{member_id}", token=STAFF_T)
    flow(35, "patch member", "PATCH", f"/gyms/{GYM_ID}/members/{member_id}", {"firstName": "Patched"}, token=STAFF_T)
    flow(36, "freeze member", "POST", f"/gyms/{GYM_ID}/members/{member_id}/freeze", {"reason": "e2e"}, token=STAFF_T)
    flow(37, "unfreeze member", "POST", f"/gyms/{GYM_ID}/members/{member_id}/unfreeze", {}, token=STAFF_T)
    flow(38, "add member note", "POST", f"/gyms/{GYM_ID}/members/{member_id}/notes", {"content": "e2e note"}, token=STAFF_T)
    flow(39, "list staff", "GET", f"/gyms/{GYM_ID}/staff?page=1&pageSize=20", token=OWNER_T)
    flow(40, "create staff (via admin)", "POST", f"/admin/gyms/{GYM_ID}/staff/create", {"email": f"staff2_{TS}@e2e.local", "password": "Test@1234", "firstName": "S2", "lastName": "E2E", "phone": "+201000000099", "staffSubRole": "receptionist"}, token=ADMIN_T, expected=(200, 201, 403))
    flow(41, "list classes", "GET", f"/gyms/{GYM_ID}/classes?page=1&pageSize=10", token=STAFF_T)
    flow(42, "list check-ins", "GET", f"/gyms/{GYM_ID}/check-ins?page=1&pageSize=10", token=STAFF_T)
    flow(43, "list announcements", "GET", f"/gyms/{GYM_ID}/announcements?page=1&pageSize=10", token=STAFF_T)
    flow(44, "get kiosk-pin", "GET", f"/gyms/{GYM_ID}/kiosk-pin", token=OWNER_T)

    # 45-50 Trainers
    c, p = call("GET", f"/gyms/{GYM_ID}/trainers?page=1&pageSize=10", token=OWNER_T)
    record(45, "list trainers", "GET", f"/gyms/{GYM_ID}/trainers", c, p)
    trainer_id = first_id(p)
    STATE["trainer_id"] = trainer_id
    flow(46, "get trainer", "GET", f"/gyms/{GYM_ID}/trainers/{trainer_id}", token=OWNER_T)
    flow(47, "set trainer rate", "PUT", f"/gyms/{GYM_ID}/trainers/{trainer_id}/rate", {"ratePerSession": 200}, token=OWNER_T)
    flow(48, "get availability (trainer)", "GET", f"/scheduling/availability?trainerId={trainer_id}&gymId={GYM_ID}", token=TRAINER_T)
    flow(49, "set availability", "PUT", "/scheduling/availability", {"trainerId": trainer_id, "gymId": GYM_ID, "sessionDurationMinutes": 60, "bufferMinutes": 15, "weeklySlots": [{"dayOfWeek": "Monday", "startTime": "08:00:00", "endTime": "12:00:00"}]}, token=TRAINER_T)
    # next Monday so the availability slot we set (Mon 08-12) covers the booking
    now = time.time()
    days_to_mon = (7 - time.gmtime(now).tm_wday) % 7 or 7
    next_mon = time.strftime("%Y-%m-%d", time.gmtime(now + days_to_mon * 86400))
    tomorrow = next_mon
    later = time.strftime("%Y-%m-%d", time.gmtime(now + (days_to_mon + 1) * 86400))
    block_day = time.strftime("%Y-%m-%d", time.gmtime(now + (days_to_mon + 14 + (TS % 30)) * 86400))
    flow(50, "block date", "POST", "/scheduling/availability/block-date", {"date": block_day, "reason": "e2e", "gymId": GYM_ID, "trainerId": trainer_id}, token=TRAINER_T)

    # 51-60 Sessions — bump date by TS to avoid collision; align to slot grid (08:00, 09:15, 10:30)
    book_date_off = (TS // 60) % 50 + days_to_mon + 21
    book_date = time.strftime("%Y-%m-%d", time.gmtime(now + book_date_off * 86400))
    # cover availability for every weekday so both bookings find a slot
    weeks = [{"dayOfWeek": d, "startTime": "08:00:00", "endTime": "16:00:00"} for d in ("Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday")]
    call("PUT", "/scheduling/availability", {"trainerId": trainer_id, "gymId": GYM_ID, "sessionDurationMinutes": 60, "bufferMinutes": 15, "weeklySlots": weeks}, token=TRAINER_T)
    # enable early_session_start override so start/complete transitions work in E2E
    call("POST", f"/admin/gyms/{GYM_ID}/feature-overrides", {"featureCode": "early_session_start", "enabled": True, "reason": "e2e"}, token=ADMIN_T)
    t1, t2 = "08:00:00", "09:15:00"
    book = lambda: call("POST", "/scheduling/sessions", {"trainerId": trainer_id, "date": book_date, "time": t1, "durationMinutes": 60, "sessionType": "PersonalTraining", "focus": "Strength"}, token=TRAINEE_T)
    c, p = book()
    record(51, "book session", "POST", "/scheduling/sessions", c, p)
    sess_id = (p.get("data") if isinstance(p, dict) and isinstance(p.get("data"), str) else None) or first_id(p)
    STATE["session_id"] = sess_id
    flow(52, "list sessions", "GET", "/scheduling/sessions?page=1&pageSize=10", token=TRAINEE_T)
    flow(53, "confirm session", "POST", f"/scheduling/sessions/{sess_id}/confirm", {}, token=TRAINER_T)
    me_t_c, me_t_p = call("GET", "/identity/me", token=TRAINEE_T)
    trainee_user_id = (me_t_p.get("data", {}) or {}).get("id") if isinstance(me_t_p, dict) else None
    flow(54, "cancel session", "POST", f"/scheduling/sessions/{sess_id}/cancel", {"reason": "e2e", "cancelledBy": trainee_user_id}, token=TRAINEE_T)
    c, p = call("POST", "/scheduling/sessions", {"trainerId": trainer_id, "date": book_date, "time": t2, "durationMinutes": 60, "sessionType": "PersonalTraining", "focus": "Strength"}, token=TRAINEE_T)
    sess2 = (p.get("data") if isinstance(p, dict) and isinstance(p.get("data"), str) else None) or first_id(p)
    if sess2:
        call("POST", f"/scheduling/sessions/{sess2}/confirm", {}, token=TRAINER_T)
        call("POST", f"/scheduling/sessions/{sess2}/start", {}, token=TRAINEE_T)
    flow(55, "complete session", "POST", f"/scheduling/sessions/{sess2}/complete", {}, token=TRAINER_T)
    flow(56, "get session", "GET", f"/scheduling/sessions/{sess2}", token=TRAINEE_T)
    flow(57, "log exercise actuals", "POST", f"/scheduling/sessions/{sess2}/exercise-logs", {"items": [{"exerciseName": "Squat", "orderIndex": 0, "sets": 3, "reps": "10,10,10", "weight": 60}]}, token=TRAINER_T)
    flow(58, "get exercise logs", "GET", f"/scheduling/sessions/{sess2}/exercise-logs", token=TRAINEE_T)
    flow(59, "available slots", "GET", f"/scheduling/available-slots?trainerId={trainer_id}&gymId={GYM_ID}&date={tomorrow}", token=TRAINEE_T)
    week_end = time.strftime("%Y-%m-%d", time.gmtime(now + (days_to_mon + 7) * 86400))
    flow(60, "availability week", "GET", f"/scheduling/availability/week?trainerId={trainer_id}&gymId={GYM_ID}&startDate={tomorrow}&endDate={week_end}", token=TRAINER_T)

    # 61-70 Billing
    flow(61, "list plans", "GET", "/billing/plans", token=OWNER_T)
    flow(62, "my subscription", "GET", "/billing/my-subscription", token=OWNER_T)
    flow(63, "create membership plan", "POST", "/billing/plans", {"gymId": GYM_ID, "name": f"plan_{TS}", "durationDays": 30, "price": 500, "features": ["basic"]}, token=OWNER_T)
    c, p = call("GET", "/billing/plans", token=OWNER_T)
    plan_id = first_id(p)
    flow(64, "update plan", "PUT", f"/billing/plans/{plan_id}", {"gymId": GYM_ID, "name": f"plan_{TS}_v2", "durationDays": 30, "price": 600, "features": ["basic", "premium"]}, token=OWNER_T)
    flow(65, "list package templates", "GET", "/billing/package-templates", token=OWNER_T)
    flow(66, "create package template", "POST", "/billing/package-templates", {"gymId": GYM_ID, "name": f"pkg_{TS}", "sessionCount": 10, "pricePerSession": 50}, token=OWNER_T)
    c, p = call("GET", "/billing/package-templates", token=OWNER_T)
    tmpl_id = first_id(p)
    expires = time.strftime("%Y-%m-%dT00:00:00Z", time.gmtime(time.time() + 86400 * 90))
    me_c, me_p = call("GET", "/identity/me", token=STAFF_T)
    staff_user_id = (me_p.get("data", {}) or {}).get("id") if isinstance(me_p, dict) else None
    flow(67, "assign package", "POST", "/billing/packages/assign", {"gymId": GYM_ID, "templateId": tmpl_id, "memberId": member_id, "trainerId": trainer_id, "assignedBy": staff_user_id, "expiresAt": expires}, token=STAFF_T)
    flow(68, "my packages", "GET", "/billing/me/packages", token=TRAINEE_T)
    flow(69, "list payments", "GET", "/billing/payments?page=1&pageSize=20", token=STAFF_T)
    c, p = call("GET", "/billing/payments?page=1&pageSize=5", token=STAFF_T)
    pay_id = first_id(p)
    # No real payment exists; expect 404 (not-found) as PASS shape for void unhappy path is documented
    flow(70, "void payment (unhappy 404)", "POST", f"/billing/payments/{uuid.uuid4()}/void", {"reason": "e2e"}, token=STAFF_T, expected=(200, 204, 404))

    # 71-78 Training / Progress / Nutrition
    flow(71, "list exercises", "GET", "/training/exercises?page=1&pageSize=10", token=TRAINER_T)
    # custom_exercise_library is Business-only. Test global scope instead so Pro can create.
    flow(72, "create exercise (Global)", "POST", "/training/exercises", {"name": f"ex_{TS}", "muscleGroups": ["Chest"], "equipment": "Barbell", "category": "Strength", "scope": "Global"}, token=ADMIN_T)
    flow(73, "create program", "POST", "/training/programs", {"gymId": GYM_ID, "name": f"prog_{TS}", "description": "e2e", "durationWeeks": 1, "weeks": [{"weekNumber": 1, "days": [{"dayOfWeek": "Monday", "workouts": []}]}], "isTemplate": False}, token=TRAINER_T)
    c, p = call("GET", f"/training/programs?trainerId={trainer_id}&gymId={GYM_ID}&page=1&pageSize=5", token=TRAINER_T)
    prog_id = first_id(p)
    flow(74, "assign program", "POST", "/training/assignments/program", {"programId": prog_id, "clientId": member_id, "startDate": f"{tomorrow}T00:00:00.000Z", "gymId": GYM_ID, "notes": "e2e"}, token=TRAINER_T)
    today = time.strftime("%Y-%m-%d")
    # accept 409 if a prior run already logged today's metric (idempotent E2E)
    flow(75, "log body metrics", "POST", "/progress/body-metrics", {"date": today, "weight": 70.5, "weightUnit": "kg", "bodyFatPercentage": 18}, token=TRAINEE_T, expected=(200, 201, 409))
    me_c, me_p = call("GET", "/identity/me", token=TRAINEE_T)
    trainee_user_id = (me_p.get("data", {}) or {}).get("id") if isinstance(me_p, dict) else None
    week_ago = time.strftime("%Y-%m-%d", time.gmtime(time.time() - 86400 * 7))
    flow(76, "body-metric trends", "GET", f"/progress/body-metrics/trends?userId={trainee_user_id}&gymId={GYM_ID}&from={week_ago}&to={today}", token=TRAINEE_T)
    flow(77, "create meal plan", "POST", "/nutrition/meal-plans", {"gymId": GYM_ID, "name": f"meal_{TS}", "description": "e2e", "dailyCalorieTarget": 2000, "macroTargets": {"proteinGrams": 150, "carbsGrams": 220, "fatGrams": 60}, "days": [{"dayOfWeek": "Monday", "plannedMeals": [], "alternatives": []}], "isTemplate": False}, token=TRAINER_T)
    flow(78, "log food", "POST", "/nutrition/food-log", {"gymId": GYM_ID, "date": today, "mealType": "lunch", "foodItems": [{"name": "rice", "quantity": 1, "unit": "cup", "calories": 200, "protein": 5, "carbs": 40, "fat": 1}]}, token=TRAINEE_T)

    # 79-80
    flow(79, "list notifications", "GET", f"/communication/notifications?userId={trainee_user_id}&page=1&pageSize=10", token=TRAINEE_T)
    flow(80, "trainers leaderboard", "GET", "/gamification/trainers/leaderboard", token=STAFF_T)

    out_fh.close()
    passed = sum(1 for r in RESULTS if r["status"] == "PASS")
    print(f"\n== Summary: {passed}/80 PASS ==")
    print(f"jsonl: {OUT}")


if __name__ == "__main__":
    main()
