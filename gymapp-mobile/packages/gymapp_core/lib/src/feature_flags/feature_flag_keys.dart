/// Feature-flag keys shared with the backend (`FeatureKeys` in .NET). Never rename a key.
abstract final class FeatureFlagKeys {
  // PT module — ON in the pt_module preset.
  static const ptTrialSessions = 'pt.trial_sessions';
  static const ptProgressMeasurements = 'pt.progress_measurements';
  static const ptTrialEndAlerts = 'pt.trial_end_alerts';
  static const ptPackages = 'pt.packages';
  static const ptDualSessionLogging = 'pt.dual_session_logging';
  static const ptRenewalReminders = 'pt.renewal_reminders';
  static const ptOwnerDashboard = 'pt.owner_dashboard';
  static const ptTrialRecap = 'pt.trial_recap';
  static const ptPackageInterest = 'pt.package_interest';
  static const ptTrialFollowUps = 'pt.trial_followups';

  // PT later phase — OFF in pt_module.
  static const ptMonthlyReport = 'pt.monthly_report';
  static const ptCoachRatings = 'pt.coach_ratings';
  static const ptCoachPerformance = 'pt.coach_performance';

  // Setting.
  static const ptAutoExpiry = 'pt.auto_expiry';

  // Every other module — OFF in pt_module.
  static const gymMemberships = 'gym.memberships';
  static const gymAttendance = 'gym.attendance';
  static const gymFinance = 'gym.finance';
  static const paymentsInApp = 'payments.in_app';
  static const classes = 'classes';
  static const schedulingBooking = 'scheduling.booking';
  static const trainingWorkouts = 'training.workouts';
  static const trainingPrograms = 'training.programs';
  static const trainingRemoteWorkout = 'training.remote_workout';
  static const nutrition = 'nutrition';
  static const progressPhotos = 'progress.photos';
  static const progressInBody = 'progress.inbody';
  static const gamificationStreaks = 'gamification.streaks';
  static const communicationChat = 'communication.chat';
  static const communicationAnnouncements = 'communication.announcements';
  static const gamificationMemberLeaderboard = 'gamification.member_leaderboard';
  static const coachLeaderboard = 'coach.leaderboard';
  static const coachTestimonials = 'coach.testimonials';
  static const analytics = 'analytics';
  static const gymMemberRecords = 'gym.member_records';
  static const gymBranding = 'gym.branding';
  static const gymDataImport = 'gym.data_import';
  static const staffCustomPermissions = 'staff.custom_permissions';
  static const integrationsApiAccess = 'integrations.api_access';

  /// What the app assumes when it has never fetched flags and cannot reach the
  /// server: the pt_module preset. Never "all on".
  static const ptModuleDefaults = <String>{
    ptTrialSessions,
    ptProgressMeasurements,
    ptTrialEndAlerts,
    ptPackages,
    ptDualSessionLogging,
    ptRenewalReminders,
    ptOwnerDashboard,
    ptTrialRecap,
    ptPackageInterest,
    ptTrialFollowUps,
  };
}
