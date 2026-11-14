namespace GymApp.Common.Application.FeatureFlags;

/// <summary>
/// Stable feature-flag keys. A key is the contract shared by the backend, the Flutter apps and the
/// web apps — never rename one; add a new key and retire the old one instead.
/// Metadata (names, module, apps) lives in <see cref="FeatureRegistry"/>.
/// </summary>
public static class FeatureKeys
{
    // --- Private training (PT) module: ON in the pt_module preset ---
    public const string PtTrialSessions = "pt.trial_sessions";
    public const string PtProgressMeasurements = "pt.progress_measurements";
    public const string PtTrialEndAlerts = "pt.trial_end_alerts";
    public const string PtPackages = "pt.packages";
    public const string PtDualSessionLogging = "pt.dual_session_logging";
    public const string PtRenewalReminders = "pt.renewal_reminders";
    public const string PtOwnerDashboard = "pt.owner_dashboard";
    public const string PtTrialRecap = "pt.trial_recap";
    public const string PtPackageInterest = "pt.package_interest";
    public const string PtTrialFollowUps = "pt.trial_followups";
    public const string PtExerciseProgress = "pt.exercise_progress";

    // --- PT, later phase: OFF in pt_module ---
    public const string PtMonthlyReport = "pt.monthly_report";
    public const string PtCoachRatings = "pt.coach_ratings";
    public const string PtCoachPerformance = "pt.coach_performance";

    // --- Settings (not features): default value comes from the registry ---
    public const string PtAutoExpiry = "pt.auto_expiry";

    // --- Every other module: OFF in pt_module ---
    public const string GymMemberships = "gym.memberships";
    public const string GymAttendance = "gym.attendance";
    public const string GymFinance = "gym.finance";
    public const string PaymentsInApp = "payments.in_app";
    public const string Classes = "classes";
    public const string SchedulingBooking = "scheduling.booking";
    public const string TrainingWorkouts = "training.workouts";
    public const string TrainingPrograms = "training.programs";
    public const string TrainingRemoteWorkout = "training.remote_workout";
    public const string Nutrition = "nutrition";
    public const string ProgressPhotos = "progress.photos";
    public const string GamificationStreaks = "gamification.streaks";
    public const string CommunicationChat = "communication.chat";
    public const string CommunicationAnnouncements = "communication.announcements";
    public const string GamificationMemberLeaderboard = "gamification.member_leaderboard";
    public const string CoachLeaderboard = "coach.leaderboard";
    public const string CoachTestimonials = "coach.testimonials";
    public const string Analytics = "analytics";
    public const string GymMemberRecords = "gym.member_records";
    public const string GymBranding = "gym.branding";
    public const string GymDataImport = "gym.data_import";
    public const string StaffCustomPermissions = "staff.custom_permissions";
    public const string IntegrationsApiAccess = "integrations.api_access";
}
