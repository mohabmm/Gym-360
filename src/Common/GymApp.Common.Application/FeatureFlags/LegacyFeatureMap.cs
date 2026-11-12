using K = GymApp.Common.Application.FeatureFlags.FeatureKeys;

namespace GymApp.Common.Application.FeatureFlags;

/// <summary>
/// Maps the legacy plan feature codes (billing.features) that existing handlers and older app
/// builds still use to the new flag keys, so <c>IGymPlanService.IsFeatureEnabledAsync</c> and
/// <c>/billing/plan-features</c> answer from the flag system.
/// </summary>
public static class LegacyFeatureMap
{
    /// <summary>Legacy codes that are part of the always-on platform core.</summary>
    private static readonly HashSet<string> AlwaysOn = new(StringComparer.Ordinal)
    {
        "member_management",
        "unlimited_trainers",
        "push_notifications",
        "multi_staff",
    };

    private static readonly Dictionary<string, string> ToFlag = new(StringComparer.Ordinal)
    {
        ["qr_checkin"] = K.GymAttendance,
        ["exercise_library_global"] = K.TrainingWorkouts,
        ["basic_reporting"] = K.Analytics,
        ["workout_builder"] = K.TrainingWorkouts,
        ["session_logging"] = K.PtDualSessionLogging,
        ["progress_tracking"] = K.PtProgressMeasurements,
        ["progress_photos"] = K.ProgressPhotos,
        ["class_management"] = K.Classes,
        ["payment_recording"] = K.GymFinance,
        ["session_packages"] = K.PtPackages,
        ["basic_analytics"] = K.Analytics,
        ["gym_branding"] = K.GymBranding,
        ["nutrition_tracking"] = K.Nutrition,
        ["meal_plans"] = K.Nutrition,
        ["member_leaderboards"] = K.GamificationMemberLeaderboard,
        ["trainer_ratings"] = K.PtCoachRatings,
        ["coach_of_the_month"] = K.CoachLeaderboard,
        ["testimonial_moderation"] = K.CoachTestimonials,
        ["advanced_analytics"] = K.Analytics,
        ["remote_workout_mode"] = K.TrainingRemoteWorkout,
        ["multi_week_programs"] = K.TrainingPrograms,
        ["white_label_branding"] = K.GymBranding,
        ["api_access"] = K.IntegrationsApiAccess,
        ["custom_exercise_library"] = K.TrainingWorkouts,
        ["custom_permissions"] = K.StaffCustomPermissions,
        ["pdf_reports"] = K.PtMonthlyReport,
        ["churn_prediction"] = K.Analytics,
        ["data_import"] = K.GymDataImport,
    };

    public enum Kind
    {
        /// <summary>Not mapped (e.g. <c>early_session_start</c> testing mode): keep the plan/override logic.</summary>
        Unmapped,
        AlwaysOn,
        Flag,
    }

    public static (Kind Kind, string? FlagKey) Lookup(string legacyCode)
    {
        if (AlwaysOn.Contains(legacyCode))
            return (Kind.AlwaysOn, null);

        return ToFlag.TryGetValue(legacyCode, out var key)
            ? (Kind.Flag, key)
            : (Kind.Unmapped, null);
    }

    /// <summary>Legacy codes that are on for a gym, given its resolved flags.</summary>
    public static IEnumerable<string> EnabledLegacyCodes(GymFeatureFlags flags) =>
        AlwaysOn.Concat(ToFlag.Where(kv => flags.IsEnabled(kv.Value)).Select(kv => kv.Key));
}
