using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Infrastructure.Seed;

/// <summary>
/// Seed data for the Billing module.
/// Contains: 4 subscription plans, 31 features, full plan-feature matrix, and storage limits.
/// </summary>
public static class BillingSeedData
{
    // Well-known plan IDs for seeding
    public static readonly Guid FreePlanId = Guid.Parse("00000000-0000-0000-0001-000000000001");
    public static readonly Guid StarterPlanId = Guid.Parse("00000000-0000-0000-0001-000000000002");
    public static readonly Guid ProPlanId = Guid.Parse("00000000-0000-0000-0001-000000000003");
    public static readonly Guid BusinessPlanId = Guid.Parse("00000000-0000-0000-0001-000000000004");

    public static IReadOnlyList<SubscriptionPlan> GetSubscriptionPlans()
    {
        return new List<SubscriptionPlan>
        {
            CreatePlan(FreePlanId, new SubscriptionPlanDetails
            {
                Code = "free",
                Name = "Free",
                MonthlyPriceEgp = 0m,
                AnnualPriceEgp = 0m,
                MemberLimit = 25,
                TrainerLimit = 1,
                StaffLimit = 1,
                Description = "Basic gym management features",
                IsPublic = true,
                SortOrder = 1
            }),
            CreatePlan(StarterPlanId, new SubscriptionPlanDetails
            {
                Code = "starter",
                Name = "Starter",
                MonthlyPriceEgp = 499m,
                AnnualPriceEgp = 4990m,
                MemberLimit = 75,
                TrainerLimit = -1,
                StaffLimit = 3,
                Description = "Essential training and management tools",
                IsPublic = true,
                SortOrder = 2
            }),
            CreatePlan(ProPlanId, new SubscriptionPlanDetails
            {
                Code = "pro",
                Name = "Pro",
                MonthlyPriceEgp = 999m,
                AnnualPriceEgp = 9990m,
                MemberLimit = 200,
                TrainerLimit = -1,
                StaffLimit = 10,
                Description = "Advanced features for growing gyms",
                IsPublic = true,
                SortOrder = 3
            }),
            CreatePlan(BusinessPlanId, new SubscriptionPlanDetails
            {
                Code = "business",
                Name = "Business",
                MonthlyPriceEgp = 1999m,
                AnnualPriceEgp = 19990m,
                MemberLimit = 500,
                TrainerLimit = -1,
                StaffLimit = -1,
                Description = "Full platform access for large operations",
                IsPublic = true,
                SortOrder = 4
            }),
        };
    }

    public static IReadOnlyList<Feature> GetFeatures()
    {
        var features = new List<(string Code, string Name, string Description, string Category, int Sort)>
        {
            ("member_management", "Member Management", "Member CRUD, search, filter", "Core", 1),
            ("qr_checkin", "Check-in System", "QR code + Member Number check-in with kiosk mode", "Core", 2),
            ("exercise_library_global", "Exercise Library", "Browse 200+ exercises", "Core", 3),
            ("basic_reporting", "Basic Reports", "Member count, check-in count", "Core", 4),
            ("unlimited_trainers", "Unlimited Trainers", "No limit on trainer accounts", "Trainers", 5),
            ("workout_builder", "Workout Builder", "Create workouts, templates, assign", "Training", 6),
            ("session_logging", "Session Logging", "Dual session logging (trainer + member)", "Training", 7),
            ("progress_tracking", "Progress Tracking", "Weight, body fat, measurements", "Progress", 8),
            ("progress_photos", "Progress Photos", "Upload and compare transformation photos", "Progress", 9),
            ("class_management", "Class Management", "Group classes, booking, waitlist", "Classes", 10),
            ("push_notifications", "Push Notifications", "Push notifications to members", "Communication", 11),
            ("payment_recording", "Payment Recording", "Record payments, generate receipts", "Billing", 12),
            ("session_packages", "Session Packages", "PT session packages with tracking", "Billing", 13),
            ("basic_analytics", "Basic Analytics", "Revenue summary, member trends", "Analytics", 14),
            ("multi_staff", "Multi-Staff", "Multiple staff accounts with fixed role permissions", "Staff", 15),
            ("gym_branding", "Gym Branding", "Custom logo and colors on member app", "Branding", 16),
            ("nutrition_tracking", "Nutrition Tracking", "Food log, water tracking", "Nutrition", 17),
            ("meal_plans", "Meal Plans", "Trainer creates and assigns meal plans", "Nutrition", 18),
            ("member_leaderboards", "Member Leaderboards", "Attendance, weight loss, consistency boards", "Gamification", 19),
            ("trainer_ratings", "Trainer Ratings", "Private client feedback on trainers", "Gamification", 20),
            ("coach_of_the_month", "Coach of the Month", "Monthly coach ranking and award", "Gamification", 21),
            ("testimonial_moderation", "Testimonials", "Anonymized client testimonials", "Gamification", 22),
            ("advanced_analytics", "Advanced Analytics", "Peak hours, retention, churn alerts", "Analytics", 23),
            ("remote_workout_mode", "Remote Workout Mode", "Trainer activates guided workouts for clients", "Training", 24),
            ("multi_week_programs", "Multi-Week Programs", "Progressive multi-week training programs", "Training", 25),
            ("white_label_branding", "White Label", "Full white-label (app name, splash, icon)", "Branding", 26),
            ("api_access", "API Access", "REST API for third-party integrations", "Integration", 27),
            ("custom_exercise_library", "Custom Exercises", "Gym adds own exercises with media", "Training", 28),
            ("custom_permissions", "Custom Permissions", "Editable permission matrix per staff role", "Staff", 29),
            ("pdf_reports", "PDF Reports", "Generate branded PDF progress reports", "Reporting", 30),
            ("churn_prediction", "Churn Prediction", "AI-powered churn risk scoring", "Analytics", 31),
            ("data_import", "Data Import", "Import members, payments, packages and InBody records from Odoo, Daftra or Excel", "Integration", 32),
            ("early_session_start", "Early Session Start (Testing Mode)", "Trainer can start a session before its scheduled time. Off by default; the StartSession endpoint enforces a 15-minute pre-window otherwise.", "Scheduling", 33),
        };

        return features.Select(f => Feature.Create(new FeatureDetails
        {
            Code = f.Code,
            Name = f.Name,
            Description = f.Description,
            Category = f.Category,
            SortOrder = f.Sort
        })).ToList();
    }

    /// <summary>
    /// Returns the full plan-feature matrix from PRODUCT_SPEC.md Section 4.
    /// Key: (planId, featureCode) -> isEnabled
    /// </summary>
    public static IReadOnlyList<PlanFeature> GetPlanFeatures(IReadOnlyList<Feature> features)
    {
        // Feature codes enabled per plan (cumulative: Business includes Pro, Pro includes Starter, etc.)
        var freeFeatures = new HashSet<string>
        {
            "member_management", "qr_checkin", "exercise_library_global", "basic_reporting"
        };

        var starterFeatures = new HashSet<string>(freeFeatures)
        {
            "unlimited_trainers", "workout_builder", "session_logging",
            "progress_tracking", "progress_photos", "class_management",
            "push_notifications", "payment_recording", "session_packages",
            "basic_analytics", "multi_staff"
        };

        var proFeatures = new HashSet<string>(starterFeatures)
        {
            "gym_branding", "nutrition_tracking", "meal_plans",
            "member_leaderboards", "trainer_ratings", "coach_of_the_month",
            "testimonial_moderation", "advanced_analytics", "remote_workout_mode",
            "multi_week_programs", "data_import"
        };

        var businessFeatures = new HashSet<string>(proFeatures)
        {
            "white_label_branding", "api_access", "custom_exercise_library",
            "custom_permissions", "pdf_reports", "churn_prediction"
        };

        var planFeatures = new List<PlanFeature>();

        var planFeatureMap = new Dictionary<Guid, HashSet<string>>
        {
            { FreePlanId, freeFeatures },
            { StarterPlanId, starterFeatures },
            { ProPlanId, proFeatures },
            { BusinessPlanId, businessFeatures }
        };

        foreach (var (planId, enabledCodes) in planFeatureMap)
        {
            foreach (var feature in features)
            {
                var isEnabled = enabledCodes.Contains(feature.Code);
                planFeatures.Add(PlanFeature.Create(planId, feature.Id, isEnabled));
            }
        }

        return planFeatures;
    }

    /// <summary>
    /// Storage limits per plan per resource type.
    /// From PRODUCT_SPEC.md Section 4.3.
    /// </summary>
    public static IReadOnlyList<PlanStorageLimit> GetPlanStorageLimits()
    {
        return new List<PlanStorageLimit>
        {
            // Total file storage (bytes): Free=100MB, Starter=500MB, Pro=2GB, Business=10GB
            PlanStorageLimit.Create(FreePlanId, "total_storage", 100L * 1024 * 1024),
            PlanStorageLimit.Create(StarterPlanId, "total_storage", 500L * 1024 * 1024),
            PlanStorageLimit.Create(ProPlanId, "total_storage", 2L * 1024 * 1024 * 1024),
            PlanStorageLimit.Create(BusinessPlanId, "total_storage", 10L * 1024 * 1024 * 1024),

            // Progress photos per member: Free=5, Starter=20, Pro=50, Business=200
            PlanStorageLimit.Create(FreePlanId, "progress_photos_per_member", 5),
            PlanStorageLimit.Create(StarterPlanId, "progress_photos_per_member", 20),
            PlanStorageLimit.Create(ProPlanId, "progress_photos_per_member", 50),
            PlanStorageLimit.Create(BusinessPlanId, "progress_photos_per_member", 200),

            // Check-in retention (days): Free=30, Starter=180, Pro=365, Business=730
            PlanStorageLimit.Create(FreePlanId, "checkin_retention_days", 30),
            PlanStorageLimit.Create(StarterPlanId, "checkin_retention_days", 180),
            PlanStorageLimit.Create(ProPlanId, "checkin_retention_days", 365),
            PlanStorageLimit.Create(BusinessPlanId, "checkin_retention_days", 730),

            // Workout-log retention (days): Free=30, Starter=365, Pro=730, Business=1825
            PlanStorageLimit.Create(FreePlanId, "workout_log_retention_days", 30),
            PlanStorageLimit.Create(StarterPlanId, "workout_log_retention_days", 365),
            PlanStorageLimit.Create(ProPlanId, "workout_log_retention_days", 730),
            PlanStorageLimit.Create(BusinessPlanId, "workout_log_retention_days", 1825),

            // Analytics retention (days): Free=7, Starter=90, Pro=365, Business=730
            PlanStorageLimit.Create(FreePlanId, "analytics_retention_days", 7),
            PlanStorageLimit.Create(StarterPlanId, "analytics_retention_days", 90),
            PlanStorageLimit.Create(ProPlanId, "analytics_retention_days", 365),
            PlanStorageLimit.Create(BusinessPlanId, "analytics_retention_days", 730),

            // Custom exercise videos: Free=0, Starter=0, Pro=0, Business=50
            PlanStorageLimit.Create(FreePlanId, "custom_exercise_videos", 0),
            PlanStorageLimit.Create(StarterPlanId, "custom_exercise_videos", 0),
            PlanStorageLimit.Create(ProPlanId, "custom_exercise_videos", 0),
            PlanStorageLimit.Create(BusinessPlanId, "custom_exercise_videos", 50),

            // API rate limit (req/min): Free=0, Starter=0, Pro=0, Business=60
            PlanStorageLimit.Create(FreePlanId, "api_rate_limit_per_min", 0),
            PlanStorageLimit.Create(StarterPlanId, "api_rate_limit_per_min", 0),
            PlanStorageLimit.Create(ProPlanId, "api_rate_limit_per_min", 0),
            PlanStorageLimit.Create(BusinessPlanId, "api_rate_limit_per_min", 60),
        };
    }

    private static SubscriptionPlan CreatePlan(Guid id, SubscriptionPlanDetails details)
    {
        // Use reflection to set the Id since SubscriptionPlan.Create generates a new one
        var plan = SubscriptionPlan.Create(details);

        // Set the well-known Id for seeding (needed for plan-feature FK references)
        typeof(GymApp.Common.Domain.Entity)
            .GetProperty(nameof(GymApp.Common.Domain.Entity.Id))!
            .SetValue(plan, id);

        return plan;
    }
}
