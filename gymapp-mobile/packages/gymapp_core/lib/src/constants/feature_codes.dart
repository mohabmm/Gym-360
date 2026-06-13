/// Feature codes matching backend billing.plan_features table.
/// Used with FeatureGate widget and planFeaturesProvider.
class FeatureCodes {
  FeatureCodes._();

  // Core (all plans)
  static const String memberManagement = 'member_management';
  static const String qrCheckIn = 'qr_check_in';
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
  static const String remoteWorkout = 'remote_workout';
  static const String multiWeekPrograms = 'multi_week_programs';
  static const String leaderboards = 'leaderboards';
  static const String trainerRatings = 'trainer_ratings';
  static const String gymBranding = 'gym_branding';
  static const String advancedAnalytics = 'advanced_analytics';
  static const String peakHoursAnalytics = 'peak_hours_analytics';
  static const String churnAlerts = 'churn_alerts';
  static const String coachLeaderboard = 'coach_leaderboard';
  static const String testimonialModeration = 'testimonial_moderation';
  static const String mealPlanBuilder = 'meal_plan_builder';

  // Business+
  static const String whiteLabel = 'white_label';
  static const String apiAccess = 'api_access';
  static const String customExerciseLibrary = 'custom_exercise_library';
  static const String pdfReports = 'pdf_reports';
  static const String churnAi = 'churn_ai';
  static const String customPermissions = 'custom_permissions';
  static const String multiLocation = 'multi_location';

  // Plan names for upgrade prompts
  static const String planFree = 'Free';
  static const String planStarter = 'Starter';
  static const String planPro = 'Pro';
  static const String planBusiness = 'Business';

  /// Returns the minimum plan required for a given feature.
  static String requiredPlanFor(String featureCode) {
    switch (featureCode) {
      case memberManagement:
      case qrCheckIn:
      case basicAnalytics:
      case basicAnnouncements:
        return planFree;
      case workoutBuilder:
      case workoutTemplates:
      case sessionBooking:
      case classManagement:
      case progressTracking:
      case sessionFeedback:
      case trainerManagement:
      case paymentRecording:
      case receipts:
      case sessionPackages:
        return planStarter;
      case nutritionTracking:
      case mealPlans:
      case remoteWorkout:
      case multiWeekPrograms:
      case leaderboards:
      case trainerRatings:
      case gymBranding:
      case advancedAnalytics:
      case peakHoursAnalytics:
      case churnAlerts:
      case coachLeaderboard:
      case testimonialModeration:
      case mealPlanBuilder:
        return planPro;
      case whiteLabel:
      case apiAccess:
      case customExerciseLibrary:
      case pdfReports:
      case churnAi:
      case customPermissions:
      case multiLocation:
        return planBusiness;
      default:
        return planBusiness;
    }
  }
}
