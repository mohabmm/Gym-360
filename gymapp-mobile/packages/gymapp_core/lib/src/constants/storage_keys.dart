/// Keys for secure storage and local cache (Hive boxes).
class StorageKeys {
  StorageKeys._();

  // Secure Storage (flutter_secure_storage)
  static const String accessToken = 'access_token';
  static const String refreshToken = 'refresh_token';
  static const String userId = 'user_id';
  static const String currentGymId = 'current_gym_id';
  static const String userRole = 'user_role';
  static const String fcmToken = 'fcm_token';

  // Per-gym secure cache (suffix with ":{gymId}")
  static const String kioskPinHashPrefix = 'kiosk_pin_hash';
  static String kioskPinHashFor(String gymId) => '$kioskPinHashPrefix:$gymId';

  // Hive boxes
  static const String userBox = 'user_box';
  static const String gymBox = 'gym_box';
  static const String planFeaturesBox = 'plan_features_box';
  static const String offlineQueueBox = 'offline_queue_box';
  static const String exerciseCacheBox = 'exercise_cache_box';
  static const String settingsBox = 'settings_box';
  static const String workoutCacheBox = 'workout_cache_box';
  static const String consentBox = 'consent_box';
  static const String telemetryQueueBox = 'telemetry_queue_box';

  // Hive keys within boxes
  static const String cachedUser = 'cached_user';
  static const String cachedGym = 'cached_gym';
  static const String cachedPlanFeatures = 'cached_plan_features';
  static const String cachedExercises = 'cached_exercises';
  static const String locale = 'locale';
  static const String themeMode = 'theme_mode';
  static const String unitSystem = 'unit_system';
  static const String leaderboardOptOut = 'leaderboard_opt_out';
  static const String consentTimestamp = 'consent_timestamp';
  static const String marketingConsent = 'marketing_consent';
  static const String dataProcessingConsent = 'data_processing_consent';
  static const String lastSyncTimestamp = 'last_sync_timestamp';
  static const String gymBranding = 'gym_branding';
  static const String notificationPreferences = 'notification_preferences';
  static const String testMode = 'test_mode';
}
