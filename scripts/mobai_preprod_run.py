#!/usr/bin/env python3
"""
Pre-prod MobAI runner — orchestrates the .mob test suites across the three
GymApp iOS simulators (trainee / trainer / staff).

Usage:
    python3 scripts/mobai_preprod_run.py
    LOOP_N=3 python3 scripts/mobai_preprod_run.py

Reads creds from `test_logs/loop_<N>_creds.json` and feeds them as ${name}
params into each login script. Requires:
- API up on localhost:8080 (bootstrap via scripts/phase0_reset.sh +
  scripts/phase1_bootstrap.sh first)
- 3 iOS simulators booted with the trainee/trainer/staff apps installed
  (mobai bridges already running — start via the mobai MCP tools)
- mobai CLI available OR mobai MCP server accessible

This script writes results to test_logs/preprod/mobai_<loop>.json and prints
a pass/fail summary. It does NOT call the MobAI MCP tools directly because
those run inside Claude Code — it emits a JSON playlist that you pipe back
into `claude --mcp-input` or invoke from a wrapper agent.

Playlist schema:
[
  {"device": "DEF8E374-...", "case_path": "01_auth/02_login_param.mob",
   "project_dir": "/.../gymapp_trainee/mobai_tests",
   "params": {"email": "...", "password": "..."} },
  ...
]
"""
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LOOP_N = os.environ.get("LOOP_N", "3")
CREDS_PATH = ROOT / "test_logs" / f"loop_{LOOP_N}_creds.json"
OUT_DIR = ROOT / "test_logs" / "preprod"
OUT_DIR.mkdir(parents=True, exist_ok=True)

# Device IDs are stable per the local sim fleet — adjust if MobAI fleet rotates.
DEVICES = {
    "trainee": "DEF8E374-B625-44D2-ABA5-EA946615BC73",
    "trainer": "BD8A7D45-949C-49AA-A79E-343802D77B97",
    "staff":   "9B4A3E95-F591-4255-8C4D-00C55CED2BF0",
}

PROJECT_DIRS = {
    "trainee": ROOT / "gymapp-mobile" / "apps" / "gymapp_trainee" / "mobai_tests",
    "trainer": ROOT / "gymapp-mobile" / "apps" / "gymapp_trainer" / "mobai_tests",
    "staff":   ROOT / "gymapp-mobile" / "apps" / "gymapp_gym" / "mobai_tests",
}

creds = json.loads(CREDS_PATH.read_text())
trainee = creds["trainee"]
trainer = creds["trainer"]
owner = creds["owner"]
staff = creds["staff"]

playlist = []

# --- TRAINEE ---
playlist += [
    {"app": "trainee", "case": "01_auth/02_login_param.mob",
     "params": {"email": trainee["email"], "password": trainee["password"]}},
    {"app": "trainee", "case": "02_home/01_loads_no_switcher.mob"},
    {"app": "trainee", "case": "03_workouts/01_workouts_screen.mob"},
    {"app": "trainee", "case": "03_workouts/02_progress_screen.mob"},
    {"app": "trainee", "case": "03_workouts/03_sessions_screen.mob"},
    {"app": "trainee", "case": "04_sessions/01_sessions_tab.mob"},
    {"app": "trainee", "case": "04_sessions/02_book_session.mob"},
    {"app": "trainee", "case": "05_water/01_log_water.mob"},
    {"app": "trainee", "case": "06_chat/01_message_trainer.mob"},
    {"app": "trainee", "case": "07_settings/01_settings_render.mob"},
    {"app": "trainee", "case": "01_auth/03_change_password.mob",
     "params": {"currentPassword": trainee["password"], "newPassword": "PreProd1!"}},
]

# --- TRAINER ---
playlist += [
    {"app": "trainer", "case": "01_auth/02_login_param.mob",
     "params": {"email": trainer["email"], "password": trainer["password"]}},
    {"app": "trainer", "case": "02_home/01_loads_no_switcher.mob"},
    {"app": "trainer", "case": "02_home/02_more_menu_no_switcher.mob"},
    {"app": "trainer", "case": "03_clients/01_clients_list.mob"},
    {"app": "trainer", "case": "03_clients/02_sessions_screen.mob"},
    {"app": "trainer", "case": "04_availability/01_set_weekly.mob"},
    {"app": "trainer", "case": "05_session_flow/01_open_today.mob"},
    {"app": "trainer", "case": "05_session_flow/02_start_with_testing_mode.mob"},
    {"app": "trainer", "case": "05_session_flow/03_complete_and_log.mob"},
    {"app": "trainer", "case": "06_settings/01_testing_mode_read_only.mob"},
]

# --- STAFF / OWNER ---
playlist += [
    {"app": "staff", "case": "01_auth/02_login_param.mob",
     "params": {"email": owner["email"], "password": owner["password"]}},
    {"app": "staff", "case": "02_dashboard/01_load.mob"},
    {"app": "staff", "case": "02_dashboard/02_kiosk_shortcut.mob"},
    {"app": "staff", "case": "03_members/01_list.mob"},
    {"app": "staff", "case": "03_members/04_add_member.mob",
     "params": {"email": f"preprod-member-{LOOP_N}@gym.local",
                "phone": "+201001112233",
                "firstName": "PreProd", "lastName": "Member"}},
    {"app": "staff", "case": "05_trainers/03_add_and_approve.mob",
     "params": {"email": f"preprod-trainer-{LOOP_N}@gym.local",
                "phone": "+201001112244",
                "firstName": "PreProd", "lastName": "Trainer"}},
    {"app": "staff", "case": "07_billing/02_assign_package.mob"},
    {"app": "staff", "case": "07_billing/05_active_packages.mob"},
    {"app": "staff", "case": "07_billing/06_assign_param.mob"},
    {"app": "staff", "case": "08_announcements/01_list.mob"},
    {"app": "staff", "case": "08_announcements/03_create_param.mob",
     "params": {"title": "Pre-prod E2E", "body": "Heavy testing run"}},
    {"app": "staff", "case": "04_checkin/01_manual_valid.mob"},
    {"app": "staff", "case": "04_checkin/05_kiosk_pin_flow.mob"},
    {"app": "staff", "case": "09_gamification/01_coach_leaderboard.mob"},
    {"app": "staff", "case": "09_gamification/02_member_leaderboard.mob"},
    {"app": "staff", "case": "11_staff/01_list.mob"},
    {"app": "staff", "case": "10_settings/02_testing_mode_toggle.mob"},
]

# Materialise full paths + device ids
out = []
for entry in playlist:
    app = entry["app"]
    out.append({
        "app": app,
        "device": DEVICES[app],
        "project_dir": str(PROJECT_DIRS[app]),
        "case_path": entry["case"],
        "params": entry.get("params", {}),
    })

PLAYLIST_PATH = OUT_DIR / f"mobai_playlist_{LOOP_N}.json"
PLAYLIST_PATH.write_text(json.dumps(out, indent=2, ensure_ascii=False))
print(f"Wrote {len(out)} entries to {PLAYLIST_PATH}")
print("Run from Claude Code:")
print("  for each entry: mcp__mobai__test_run("
      "project_dir=entry.project_dir, "
      "case_path=entry.case_path, "
      "device_id=entry.device, "
      "params=entry.params)")
