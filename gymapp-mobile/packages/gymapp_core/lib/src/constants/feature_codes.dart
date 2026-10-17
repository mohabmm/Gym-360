/// Feature codes matching backend billing.plan_features table.
/// Used with FeatureGate widget and planFeaturesProvider.
class FeatureCodes {
  FeatureCodes._();

  // Core (all plans)
  static const String memberManagement = 'member_management';
  static const String qrCheckIn = 'qr_checkin';
  static const String basicAnalytics = 'basic_analytics';
  static const String basicAnnouncements = 'basic_announcements';

  // Starter+
  static const String workoutBuilder = 'workout_builder';
  static const String workoutTemplates = 'workout_templates';
  static const String sessionBooking = 'session_booking';
  static const String classManagement = 'class_management';
  static const String progressTracking = 'progress_tracking';
  static const String sessionFeedback = 'session_feedback';
  static const String trainerManagement = 'trainer_management';
  static const String paymentRecording = 'payment_recording';
  static const String receipts = 'receipts';
  static const String sessionPackages = 'session_packages';

  // Pro+
  static const String nutritionTracking = 'nutrition_tracking';
  static const String mealPlans = 'meal_plans';
  static const String remoteWorkout = 'remote_workout_mode';
  static const String multiWeekPrograms = 'multi_week_programs';
  static const String leaderboards = 'member_leaderboards';
  static const String trainerRatings = 'trainer_ratings';
  static const String gymBranding = 'gym_branding';
  static const String advancedAnalytics = 'advanced_analytics';
  static const String peakHoursAnalytics = 'peak_hours_analytics';
  static const String churnAlerts = 'churn_alerts';
  static const String coachLeaderboard = 'coach_of_the_month';
  static const String testimonialModeration = 'testimonial_moderation';
  static const String mealPlanBuilder = 'meal_plan_builder';

  // Business+
  static const String whiteLabel = 'white_label_branding';
  static const String apiAccess = 'api_access';
  static const String customExerciseLibrary = 'custom_exercise_library';
  static const String pdfReports = 'pdf_reports';
  static const String churnAi = 'churn_prediction';
  static const String customPermissions = 'custom_permissions';
  static const String multiLocation = 'multi_location';

  // Plan names for upgrade prompts
  static const String planFree = 'Free';
  static const String planStarter = 'Starter';
  static const String planPro = 'Pro';
  static const String planBusiness = 'Business';

  /// Minimum plan per feature. Unknown features default to [planBusiness].
  static const Map<String, String> _minimumPlan = {
    memberManagement: planFree,
    qrCheckIn: planFree,
    basicAnalytics: planFree,
    basicAnnouncements: planFree,
    workoutBuilder: planStarter,
    workoutTemplates: planStarter,
    sessionBooking: planStarter,
    classManagement: planStarter,
    progressTracking: planStarter,
    sessionFeedback: planStarter,
    trainerManagement: planStarter,
    paymentRecording: planStarter,
    receipts: planStarter,
    sessionPackages: planStarter,
    nutritionTracking: planPro,
    mealPlans: planPro,
    remoteWorkout: planPro,
    multiWeekPrograms: planPro,
    leaderboards: planPro,
    trainerRatings: planPro,
    gymBranding: planPro,
    advancedAnalytics: planPro,
    peakHoursAnalytics: planPro,
    churnAlerts: planPro,
    coachLeaderboard: planPro,
    testimonialModeration: planPro,
    mealPlanBuilder: planPro,
    whiteLabel: planBusiness,
    apiAccess: planBusiness,
    customExerciseLibrary: planBusiness,
    pdfReports: planBusiness,
    churnAi: planBusiness,
    customPermissions: planBusiness,
    multiLocation: planBusiness,
  };

  /// Returns the minimum plan required for a given feature.
  static String requiredPlanFor(String featureCode) =>
      _minimumPlan[featureCode] ?? planBusiness;
}
