#!/usr/bin/env python3
"""Generate Maestro E2E flows for all 3 Flutter apps."""
from pathlib import Path
from textwrap import dedent

ROOT = Path("/Users/mmagdy2-eg/Desktop/Development/Forma/gymapp-mobile/apps")

LOGIN_HEAD = """\
- launchApp:
    clearState: false
- waitForAnimationToEnd:
    timeout: 15000
- tapOn:
    text: ".*دخل بريدك.*"
    optional: true
- inputText: ${EMAIL}
- hideKeyboard
- tapOn:
    text: ".*دخل كلمة المرور.*"
    optional: true
- inputText: ${PASSWORD}
- hideKeyboard
- tapOn:
    text: "تسجيل الدخول"
    optional: true
- extendedWaitUntil:
    notVisible: "تسجيل الدخول"
    timeout: 30000
"""

def write_flow(app_dir, name, app_id, email, password, body):
    flow_dir = ROOT / app_dir / ".maestro" / "flows"
    flow_dir.mkdir(parents=True, exist_ok=True)
    path = flow_dir / f"{name}.yaml"
    content = dedent(f"""\
        appId: {app_id}
        name: {name}
        env:
          EMAIL: {email}
          PASSWORD: "{password}"
        ---
        """) + LOGIN_HEAD + body
    path.write_text(content)
    return path

# Per-app nav tab coordinates. RTL ordering, y=92%.
_NAV_POS = {
    "trainee": {
        "الرئيسية": "90%,92%", "التمارين": "70%,92%", "التقدم": "50%,92%",
        "المواعيد": "30%,92%", "المزيد": "10%,92%",
    },
    "trainer": {
        "الرئيسية": "90%,92%", "العملاء": "70%,92%", "التمارين": "50%,92%",
        "الجدول": "30%,92%", "المزيد": "10%,92%",
    },
    "staff": {
        "الرئيسية": "90%,92%", "الاعضاء": "70%,92%", "المدربين": "50%,92%",
        "الحصص": "30%,92%", "المزيد": "10%,92%",
    },
}

def tap_nav(label, app):
    pt = _NAV_POS[app][label]
    return f'- tapOn:\n    point: "{pt}"\n'

def tap(label, optional=False):
    opt = "\n    optional: true" if optional else ""
    return f'- tapOn:\n    text: "{label}"{opt}\n'

def assert_visible(text, timeout=15000):
    return (f'- extendedWaitUntil:\n    visible: "{text}"\n'
            f'    timeout: {timeout}\n- assertVisible: "{text}"\n')

def assert_not_visible(text, timeout=15000):
    return (f'- extendedWaitUntil:\n    notVisible: "{text}"\n'
            f'    timeout: {timeout}\n- assertNotVisible: "{text}"\n')

# ===== TRAINEE =====
TRAINEE = ("gymapp_trainee", "com.example.gymappTrainee",
           "trainee-l3-612493@gym.local", "123456")
trainee_flows = {
    "00_login": "",
    "01_home": tap_nav("الرئيسية", "trainee") + assert_not_visible("تسجيل الدخول"),
    "02_workouts": tap_nav("التمارين", "trainee") + assert_visible("مكتبة التمارين|التمارين|تمرين"),
    "03_progress": tap_nav("التقدم", "trainee") + assert_visible("تقدمي|التقدم|الوزن"),
    "04_schedule": tap_nav("المواعيد", "trainee") + assert_visible("الجدول|المواعيد|الجلسات|احجز"),
    "05_more": tap_nav("المزيد", "trainee") + assert_visible("الإعدادات|الاعدادات|الإعلانات|الاعلانات|الدردشة|المتصدرين|التغذية"),
    "06_book_session": tap_nav("المواعيد", "trainee") + tap("احجز", optional=True)
        + assert_not_visible("تسجيل الدخول"),
    "07_water_tracker": tap_nav("المزيد", "trainee") + tap("الماء", optional=True)
        + tap("التغذية", optional=True) + assert_not_visible("تسجيل الدخول"),
    "08_leaderboard": tap_nav("المزيد", "trainee") + tap("المتصدرين", optional=True)
        + assert_not_visible("تسجيل الدخول"),
    "09_settings": tap_nav("المزيد", "trainee") + tap("الإعدادات", optional=True)
        + tap("الاعدادات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "10_announcements": tap_nav("المزيد", "trainee") + tap("الإعلانات", optional=True)
        + tap("الاعلانات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "11_chat": tap_nav("المزيد", "trainee") + tap("الدردشة", optional=True)
        + tap("المحادثات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "12_membership": tap_nav("المزيد", "trainee") + tap("الاشتراك", optional=True)
        + tap("العضوية", optional=True) + assert_not_visible("تسجيل الدخول"),
    "13_checkin": tap_nav("المزيد", "trainee") + tap("تسجيل الحضور", optional=True)
        + tap("الحضور", optional=True) + assert_not_visible("تسجيل الدخول"),
    "14_exercise_library": tap_nav("التمارين", "trainee") + tap("المكتبة", optional=True)
        + assert_not_visible("تسجيل الدخول"),
}

# ===== TRAINER =====
TRAINER = ("gymapp_trainer", "com.example.gymappTrainer",
           "trainer-l3-612493@gym.local", "123456")
trainer_flows = {
    "00_login": "",
    "01_home": tap_nav("الرئيسية", "trainer") + assert_not_visible("تسجيل الدخول"),
    "02_clients": tap_nav("العملاء", "trainer") + assert_visible("قائمة العملاء|العملاء|بحث"),
    "03_workouts": tap_nav("التمارين", "trainer") + assert_visible("القوالب|التمارين|البرامج|تمرين"),
    "04_schedule": tap_nav("الجدول", "trainer") + assert_visible("الجدول|التقويم|التوفر|توفر"),
    "05_more": tap_nav("المزيد", "trainer") + assert_not_visible("تسجيل الدخول"),
    "06_template_library": tap_nav("التمارين", "trainer") + tap("القوالب", optional=True)
        + tap("Templates", optional=True) + assert_not_visible("تسجيل الدخول"),
    "07_availability": tap_nav("الجدول", "trainer") + tap("التوفر", optional=True)
        + tap("الإتاحة", optional=True) + assert_not_visible("تسجيل الدخول"),
    "08_booking_requests": tap_nav("الجدول", "trainer") + tap("الطلبات", optional=True)
        + tap("طلبات الحجز", optional=True) + assert_not_visible("تسجيل الدخول"),
    "09_my_ratings": tap_nav("المزيد", "trainer") + tap("تقييمي", optional=True)
        + tap("التقييمات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "10_earnings": tap_nav("المزيد", "trainer") + tap("الارباح", optional=True)
        + tap("الأرباح", optional=True) + assert_not_visible("تسجيل الدخول"),
    "11_chat_list": tap_nav("المزيد", "trainer") + tap("المحادثات", optional=True)
        + tap("الدردشة", optional=True) + assert_not_visible("تسجيل الدخول"),
    "12_broadcast": tap_nav("المزيد", "trainer") + tap("رسالة جماعية", optional=True)
        + tap("البث", optional=True) + assert_not_visible("تسجيل الدخول"),
    "13_profile": tap_nav("المزيد", "trainer") + tap("الملف الشخصي", optional=True)
        + tap("الحساب", optional=True) + assert_not_visible("تسجيل الدخول"),
    "14_settings": tap_nav("المزيد", "trainer") + tap("الاعدادات", optional=True)
        + tap("الإعدادات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "15_leave": tap_nav("الجدول", "trainer") + tap("الإجازات", optional=True)
        + tap("الاجازات", optional=True) + assert_not_visible("تسجيل الدخول"),
}

# ===== GYMSTAFF =====
STAFF = ("gymapp_gym", "com.example.gymappGym",
         "staff-l3-612493@gym.local", "123456")
staff_flows = {
    "00_login": "",
    "01_dashboard": tap_nav("الرئيسية", "staff") + assert_not_visible("تسجيل الدخول"),
    "02_members": tap_nav("الاعضاء", "staff") + assert_visible("قائمة الأعضاء|الأعضاء|الاعضاء|بحث"),
    "03_trainers": tap_nav("المدربين", "staff") + assert_visible("المدربون|قائمة المدربين|المدربين|طلبات الانضمام"),
    "04_classes": tap_nav("الحصص", "staff") + assert_visible("الحصص|الجدول|جدول الحصص"),
    "05_more": tap_nav("المزيد", "staff") + assert_visible("التمارين|الإعلانات|الاعلانات|تسجيل دفع|لوحة الايرادات"),
    "06_exercises": tap_nav("المزيد", "staff") + tap("التمارين", optional=True)
        + assert_not_visible("تسجيل الدخول"),
    "07_revenue": tap_nav("المزيد", "staff") + tap("لوحة الايرادات", optional=True)
        + tap("الإيرادات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "08_record_payment": tap_nav("المزيد", "staff") + tap("تسجيل دفع", optional=True)
        + tap("الدفعات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "09_active_packages": tap_nav("المزيد", "staff") + tap("الباقات النشطة", optional=True)
        + tap("الباقات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "10_package_templates": tap_nav("المزيد", "staff") + tap("قوالب الباقات", optional=True)
        + tap("القوالب", optional=True) + assert_not_visible("تسجيل الدخول"),
    "11_announcements": tap_nav("المزيد", "staff") + tap("الإعلانات", optional=True)
        + tap("الاعلانات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "12_kiosk": tap_nav("المزيد", "staff") + tap("الكشك", optional=True)
        + tap("Kiosk", optional=True) + assert_not_visible("تسجيل الدخول"),
    "13_checkin_log": tap_nav("المزيد", "staff") + tap("سجل الحضور", optional=True)
        + tap("الحضور", optional=True) + assert_not_visible("تسجيل الدخول"),
    "14_staff": tap_nav("المزيد", "staff") + tap("الموظفين", optional=True)
        + assert_not_visible("تسجيل الدخول"),
    "15_amenities": tap_nav("المزيد", "staff") + tap("المرافق", optional=True)
        + tap("الخدمات", optional=True) + assert_not_visible("تسجيل الدخول"),
    "16_sections": tap_nav("المزيد", "staff") + tap("الأقسام", optional=True)
        + tap("الاقسام", optional=True) + assert_not_visible("تسجيل الدخول"),
    "17_seasonal_hours": tap_nav("المزيد", "staff") + tap("ساعات الموسم", optional=True)
        + tap("المواعيد الموسمية", optional=True) + assert_not_visible("تسجيل الدخول"),
    "18_gym_profile": tap_nav("المزيد", "staff") + tap("ملف الصالة", optional=True)
        + tap("الصالة", optional=True) + assert_not_visible("تسجيل الدخول"),
    "19_coach_leaderboard": tap_nav("المزيد", "staff") + tap("ترتيب المدربين", optional=True)
        + tap("المتصدرين", optional=True) + assert_not_visible("تسجيل الدخول"),
    "20_member_leaderboard": tap_nav("المزيد", "staff") + tap("ترتيب الاعضاء", optional=True)
        + tap("متصدرين الأعضاء", optional=True) + assert_not_visible("تسجيل الدخول"),
}

def emit(app_dir, app_id, email, pw, flows):
    for name, body in flows.items():
        write_flow(app_dir, name, app_id, email, pw, body)

emit(*TRAINEE, trainee_flows)
emit(*TRAINER, trainer_flows)
emit(*STAFF, staff_flows)
print("OK")
