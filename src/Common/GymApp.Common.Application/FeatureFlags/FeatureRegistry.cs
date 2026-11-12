using K = GymApp.Common.Application.FeatureFlags.FeatureKeys;

namespace GymApp.Common.Application.FeatureFlags;

/// <summary>
/// The single code-defined list of feature flags. It is upserted into <c>feature_flags.features</c>
/// on startup; a new entry here shows up in the Super Admin dashboard and in every gym's resolved
/// flags (OFF unless a preset or override turns it on).
/// Always-on platform areas (auth, profiles, coach assignment, notifications, settings) are not flags.
/// </summary>
public static class FeatureRegistry
{
    public const string PtModulePreset = "pt_module";
    public const string BusinessPreset = "business";

    private const FeatureApps Mobile = FeatureApps.Trainee | FeatureApps.Trainer;
    private const FeatureApps Staff = FeatureApps.StaffMobile | FeatureApps.StaffWeb;

    public static IReadOnlyList<FeatureDefinition> All { get; } = new List<FeatureDefinition>
    {
        // ---- PT module (ON) ----
        Pt(K.PtTrialSessions, FeatureApps.All,
            "Trial sessions", "الحصص التجريبية",
            "Free trial session package; the desk assigns a coach.",
            "باقة حصص تجريبية مجانية، ويحدد الاستقبال المدرب."),
        Pt(K.PtProgressMeasurements, FeatureApps.All,
            "Progress measurements", "قياسات التقدم",
            "Coach records measurements from session 1; trainee sees progress.",
            "يسجل المدرب القياسات من الحصة الأولى ويرى المتدرب تقدمه."),
        Pt(K.PtTrialEndAlerts, FeatureApps.Trainer | Staff,
            "Trial end alerts", "تنبيهات نهاية التجربة",
            "Alert coach and desk at the last free session, plus a 'finished trial, not converted' list.",
            "تنبيه المدرب والاستقبال عند آخر حصة مجانية، وقائمة بمن أنهى التجربة ولم يشترك."),
        Pt(K.PtPackages, FeatureApps.All,
            "PT packages", "باقات التدريب الشخصي",
            "Paid PT packages with a remaining-sessions counter.",
            "باقات تدريب شخصي مدفوعة مع عداد الحصص المتبقية."),
        Pt(K.PtDualSessionLogging, Mobile | Staff,
            "Session logging", "تسجيل الحصص",
            "Coach logs a session in one or two taps; trainee confirms.",
            "يسجل المدرب الحصة بضغطة أو ضغطتين ويؤكدها المتدرب."),
        Pt(K.PtRenewalReminders, FeatureApps.All,
            "Renewal reminders", "تذكيرات التجديد",
            "Reminders at 3 sessions left and 1 session left.",
            "تذكير عند بقاء ٣ حصص وعند بقاء حصة واحدة."),
        Pt(K.PtOwnerDashboard, Staff,
            "PT owner dashboard", "لوحة المالك للتدريب الشخصي",
            "Trial-to-paid conversion per coach, renewals and sessions logged.",
            "نسبة التحويل من التجربة إلى الاشتراك لكل مدرب، والتجديدات، والحصص المسجلة."),

        // ---- PT later phase (OFF) ----
        Off(K.PtMonthlyReport, "pt", FeatureApps.All,
            "Monthly progress report", "تقرير التقدم الشهري",
            "Monthly progress report with the gym logo, shareable on WhatsApp.",
            "تقرير تقدم شهري بشعار النادي قابل للمشاركة على واتساب."),
        Off(K.PtCoachRatings, "pt", FeatureApps.Trainee | Staff | FeatureApps.Trainer,
            "Coach ratings", "تقييمات المدربين",
            "Anonymous combined coach ratings, owner only, shown after at least 5 ratings.",
            "تقييمات مجمعة ومجهولة للمدربين، للمالك فقط، تظهر بعد ٥ تقييمات على الأقل."),
        Off(K.PtCoachPerformance, "pt", Staff,
            "Coach performance", "أداء المدرب",
            "Per-coach page with conversion, renewals and sessions.",
            "صفحة لكل مدرب بنسبة التحويل والتجديدات والحصص."),

        // ---- Settings ----
        new(K.PtAutoExpiry, "pt",
            new FeatureText("Auto-expire packages", "انتهاء الباقات تلقائيًا",
                "When on, packages with an expiry date expire automatically. When off, sessions never expire.",
                "عند التفعيل تنتهي الباقات ذات تاريخ الانتهاء تلقائيًا. عند الإيقاف لا تنتهي الحصص أبدًا."),
            Staff, InPtModule: false, FeatureKind.Setting),

        // ---- Every other module (OFF in pt_module) ----
        Off(K.GymMemberships, "gym", FeatureApps.Trainee | Staff,
            "Memberships", "الاشتراكات",
            "Membership plans, balances, freeze and unfreeze.",
            "خطط العضوية والأرصدة والتجميد."),
        Off(K.GymAttendance, "gym", FeatureApps.Trainee | Staff,
            "Attendance & check-in", "الحضور وتسجيل الدخول",
            "QR and manual check-in, check-in log and kiosk.",
            "تسجيل الدخول بالرمز أو يدويًا وسجل الحضور وجهاز الاستقبال."),
        Off(K.GymFinance, "gym", FeatureApps.Trainer | Staff,
            "Finance", "المالية",
            "Payments, receipts, revenue dashboards and trainer earnings.",
            "المدفوعات والإيصالات ولوحات الإيرادات وأرباح المدربين."),
        Off(K.PaymentsInApp, "payments", FeatureApps.Trainee,
            "In-app payments", "الدفع داخل التطبيق",
            "Online payment inside the apps (no gateway is integrated yet).",
            "الدفع الإلكتروني داخل التطبيقات (لا توجد بوابة دفع مدمجة بعد)."),
        Off(K.Classes, "classes", FeatureApps.Trainee | Staff,
            "Group classes", "الحصص الجماعية",
            "Class schedule, booking, waitlist and attendance.",
            "جدول الحصص الجماعية والحجز وقائمة الانتظار والحضور."),
        Off(K.SchedulingBooking, "scheduling", Mobile,
            "Session booking & availability", "حجز الحصص والمواعيد",
            "Trainee self-booking, coach availability, recurring sessions and leave.",
            "حجز المتدرب للحصص ومواعيد المدرب والحصص المتكررة والإجازات."),
        Off(K.TrainingWorkouts, "training", FeatureApps.All,
            "Workouts & exercise library", "التمارين ومكتبة التمارين",
            "Workout builder, templates, assignments, workout logs and exercises.",
            "إنشاء التمارين والقوالب والتعيين وسجل التمارين ومكتبة التمارين."),
        Off(K.TrainingPrograms, "training", Mobile,
            "Multi-week programs", "البرامج متعددة الأسابيع",
            "Progressive multi-week training programs.",
            "برامج تدريبية متدرجة لعدة أسابيع."),
        Off(K.TrainingRemoteWorkout, "training", Mobile,
            "Remote workout", "التمرين عن بعد",
            "Coach activates guided workouts for clients.",
            "يفعّل المدرب تمارين موجهة لعملائه."),
        Off(K.Nutrition, "nutrition", Mobile,
            "Nutrition", "التغذية",
            "Food log, water tracking and meal plans.",
            "سجل الطعام وتتبع الماء والخطط الغذائية."),
        Off(K.ProgressPhotos, "progress", Mobile,
            "Progress photos", "صور التقدم",
            "Upload and compare transformation photos.",
            "رفع صور التحول ومقارنتها."),
        Off(K.GamificationStreaks, "gamification", FeatureApps.Trainee,
            "Streaks", "سلاسل الالتزام",
            "Attendance streaks and milestone notifications.",
            "سلاسل الالتزام وإشعارات الإنجاز."),
        Off(K.CommunicationChat, "communication", Mobile | FeatureApps.StaffWeb,
            "Chat", "المحادثات",
            "In-app chat between trainees, coaches and staff.",
            "المحادثات داخل التطبيق بين المتدربين والمدربين والموظفين."),
        Off(K.CommunicationAnnouncements, "communication", FeatureApps.All,
            "Announcements & broadcast", "الإعلانات والرسائل الجماعية",
            "Gym announcements and coach broadcast messages.",
            "إعلانات النادي ورسائل المدرب الجماعية."),
        Off(K.GamificationMemberLeaderboard, "gamification", FeatureApps.Trainee | Staff,
            "Member leaderboards", "لوحات صدارة الأعضاء",
            "Attendance, weight-loss and consistency leaderboards.",
            "لوحات صدارة للحضور وفقدان الوزن والالتزام."),
        Off(K.CoachLeaderboard, "coach", FeatureApps.Trainer | Staff,
            "Coach leaderboard", "لوحة صدارة المدربين",
            "Monthly coach scores and coach of the month.",
            "نقاط المدربين الشهرية ومدرب الشهر."),
        Off(K.CoachTestimonials, "coach", FeatureApps.Trainee | Staff,
            "Testimonials", "آراء العملاء",
            "Client testimonials and moderation.",
            "آراء العملاء ومراجعتها."),
        Off(K.Analytics, "analytics", Staff,
            "Analytics", "التحليلات",
            "Gym analytics, peak hours, churn and the multi-gym owner view.",
            "تحليلات النادي وأوقات الذروة ومعدل الانسحاب ولوحة المالك متعددة الفروع."),
        Off(K.GymMemberRecords, "gym", FeatureApps.Trainer | Staff,
            "Member notes & onboarding", "ملاحظات العضو واستبيان البداية",
            "Coach and staff notes and the onboarding questionnaire.",
            "ملاحظات المدرب والموظفين واستبيان بداية الاشتراك."),
        Off(K.GymBranding, "gym", FeatureApps.All,
            "Branding", "الهوية البصرية",
            "Custom logo, colors and white label.",
            "شعار وألوان مخصصة وعلامة خاصة."),
        Off(K.GymDataImport, "gym", FeatureApps.StaffWeb,
            "Data import", "استيراد البيانات",
            "Import members, payments and packages from Excel, Odoo or Daftra.",
            "استيراد الأعضاء والمدفوعات والباقات من إكسل أو أودو أو دفترة."),
        Off(K.StaffCustomPermissions, "staff", Staff,
            "Custom permissions", "صلاحيات مخصصة",
            "Editable permission matrix per staff role.",
            "مصفوفة صلاحيات قابلة للتعديل لكل دور."),
        Off(K.IntegrationsApiAccess, "integrations", FeatureApps.None,
            "API access", "الوصول عبر API",
            "REST API for third-party integrations.",
            "واجهة برمجية للتكامل مع أنظمة أخرى."),
    };

    private static readonly Dictionary<string, FeatureDefinition> ByKey =
        All.ToDictionary(d => d.Key, StringComparer.Ordinal);

    public static bool IsKnown(string key) => ByKey.ContainsKey(key);

    public static FeatureDefinition? Find(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>Keys the <c>pt_module</c> preset turns on: also the client-side fail-safe defaults.</summary>
    public static IReadOnlyList<string> PtModuleDefaults { get; } =
        All.Where(d => d.InPtModule).Select(d => d.Key).ToList();

    private static FeatureDefinition Pt(
        string key, FeatureApps apps, string nameEn, string nameAr, string descEn, string descAr) =>
        new(key, "pt", new FeatureText(nameEn, nameAr, descEn, descAr), apps, InPtModule: true);

    private static FeatureDefinition Off(
        string key, string module, FeatureApps apps, string nameEn, string nameAr, string descEn, string descAr) =>
        new(key, module, new FeatureText(nameEn, nameAr, descEn, descAr), apps, InPtModule: false);
}
