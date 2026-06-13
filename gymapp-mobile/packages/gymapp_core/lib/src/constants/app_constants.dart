/// Application-wide constants used across all Gym 360 apps.
class AppConstants {
  AppConstants._();

  // App info
  static const String appName = 'Gym 360';
  static const String appNameAr = 'جيم 360';

  // API
  // Read/send timeout. Raised from 15s: a cold backend (first request after
  // the staging/host instance has been idle) can take >15s to answer the
  // initial /identity/profile on the login path, which aborted with a
  // receiveTimeout and surfaced as a spurious "please log in again" error.
  static const int apiTimeoutSeconds = 30;
  static const int apiConnectTimeoutSeconds = 20;
  static const int paginationPageSize = 20;

  // Auth
  static const int otpLength = 6;
  static const int otpExpiryMinutes = 5;
  static const int gymCodeLength = 6;
  static const int refreshTokenExpiryDays = 30;
  static const int accessTokenExpiryMinutes = 60;

  // Offline queue
  static const int offlineQueueRetentionDays = 7;
  static const int maxOfflineQueueItems = 500;

  // Kiosk
  static const int kioskAutoResetSeconds = 5;
  static const int kioskMemberNumberMaxLength = 8;

  // Progress
  static const int maxProgressPhotosDefault = 20;
  static const int minPhotoSizeKb = 50;
  static const int maxPhotoSizeMb = 10;

  // Body metrics
  static const int bodyMeasurementCount = 9;

  // Ratings
  static const int minRating = 1;
  static const int maxRating = 5;
  static const int minRpe = 1;
  static const int maxRpe = 10;

  // Workout
  static const int defaultRestTimerSeconds = 60;
  static const int maxSetsPerExercise = 20;
  static const int maxExercisesPerWorkout = 30;

  // Nutrition
  static const int defaultWaterGoalMl = 2500;
  static const int waterIncrementMl = 250;
  static const int mealPlanDays = 7;

  // UI
  static const double borderRadius = 12.0;
  static const double borderRadiusSmall = 8.0;
  static const double borderRadiusLarge = 16.0;
  static const double cardElevation = 2.0;
  static const double iconSizeSmall = 16.0;
  static const double iconSizeMedium = 24.0;
  static const double iconSizeLarge = 32.0;
  static const double avatarSizeSmall = 32.0;
  static const double avatarSizeMedium = 48.0;
  static const double avatarSizeLarge = 64.0;
  static const double bottomNavHeight = 60.0;

  // Animation
  static const int animationDurationMs = 300;
  static const int shimmerDurationMs = 1500;
  static const int snackbarDurationMs = 3000;

  // Scheduling
  static const int defaultBufferMinutes = 15;
  static const int sessionDurationMinutes = 60;
  static const int maxSlotsPerDay = 12;

  // Leaderboard types
  static const String leaderboardAttendance = 'attendance';
  static const String leaderboardWeightLoss = 'weight_loss';
  static const String leaderboardConsistency = 'consistency';

  // Supported locales
  static const String localeAr = 'ar';
  static const String localeEn = 'en';
  static const String defaultLocale = localeAr;

  // Currency
  static const String currency = 'EGP';
  static const String currencyAr = 'ج.م';
  static const String currencySymbol = 'E\u00a3';

  // Date formats
  static const String dateFormatShort = 'dd/MM/yyyy';
  static const String dateFormatLong = 'dd MMMM yyyy';
  static const String timeFormat24 = 'HH:mm';
  static const String timeFormat12 = 'hh:mm a';
  static const String dateTimeFormat = 'dd/MM/yyyy HH:mm';

  // FCM topics
  static const String fcmTopicGlobal = 'global';
  static String fcmTopicGym(String gymId) => 'gym_$gymId';
  static String fcmTopicUser(String userId) => 'user_$userId';

  // Gym sections
  static const String sectionMens = 'mens';
  static const String sectionWomens = 'womens';
  static const String sectionMixed = 'mixed';

  // Staff roles
  static const String roleOwner = 'owner';
  static const String roleManager = 'manager';
  static const String roleReceptionist = 'receptionist';

  // Payment methods
  static const String paymentCash = 'cash';
  static const String paymentCard = 'card';
  static const String paymentOnline = 'online';
  static const String paymentBankTransfer = 'bank_transfer';

  // Payment purposes
  static const String purposeMembership = 'membership';
  static const String purposePackage = 'package';

  // Member statuses
  static const String memberActive = 'active';
  static const String memberFrozen = 'frozen';
  static const String memberExpired = 'expired';
  static const String memberPending = 'pending';

  // Session statuses
  static const String sessionScheduled = 'scheduled';
  static const String sessionCompleted = 'completed';
  static const String sessionCancelled = 'cancelled';
  static const String sessionNoShow = 'no_show';
  static const String sessionLateCancelled = 'late_cancelled';
}
