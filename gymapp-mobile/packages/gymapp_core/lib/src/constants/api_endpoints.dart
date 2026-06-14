/// All API endpoint paths used across the GymApp platform.
/// Base URL is configured per environment in ApiClient.
class ApiEndpoints {
  ApiEndpoints._();

  static const String apiVersion = 'v1';
  static const String basePrefix = '/api/$apiVersion';

  // Observability — client telemetry ingest (mobile → SigNoz via backend).
  static const String clientLogs = '$basePrefix/observability/client-logs';

  // Identity
  static const String login = '$basePrefix/identity/login';
  static const String register = '$basePrefix/identity/register';
  static const String refreshToken = '$basePrefix/identity/refresh-token';
  static const String sendOtp = '$basePrefix/identity/send-otp';
  static const String verifyOtp = '$basePrefix/identity/verify-otp';
  static const String profile = '$basePrefix/identity/profile';
  static const String linkGym = '$basePrefix/identity/link-gym';
  static const String myData = '$basePrefix/identity/my-data';
  static const String deleteAccount = '$basePrefix/identity/account';
  static const String consent = '$basePrefix/identity/consent';
  static const String forgotPassword = '$basePrefix/identity/forgot-password';
  static const String resetPassword = '$basePrefix/identity/reset-password';
  static const String changePassword = '$basePrefix/identity/change-password';
  static const String socialLogin = '$basePrefix/identity/social-login';
  static const String identityStaffCreateUser =
      '$basePrefix/identity/users/staff-create';
  static const String identityStaffLookupUser =
      '$basePrefix/identity/users/staff-lookup';
  static const String identityStaffLinkExisting =
      '$basePrefix/identity/users/staff-link-existing';

  // Gyms
  static const String gyms = '$basePrefix/gyms';
  static String gymById(String id) => '$basePrefix/gyms/$id';
  static String gymBranding(String id) => '$basePrefix/gyms/$id/branding';
  static String gymOperatingHours(String id) =>
      '$basePrefix/gyms/$id/operating-hours';
  static String gymSeasonalHours(String id) =>
      '$basePrefix/gyms/$id/seasonal-hours';
  static String gymSections(String id) => '$basePrefix/gyms/$id/sections';
  static String gymSessionPolicy(String id) =>
      '$basePrefix/gyms/$id/session-policy';

  @Deprecated('BE endpoint is /session-policy (singular). Use gymSessionPolicy(id).')
  static String gymSessionPolicies(String id) =>
      '$basePrefix/gyms/$id/session-policy';
  static String gymAmenities(String id) => '$basePrefix/gyms/$id/amenities';
  static String gymStaff(String id) => '$basePrefix/gyms/$id/staff';
  static String gymSettings(String id) => '$basePrefix/gyms/$id/settings';
  static String gymTestingMode(String gymId) =>
      '$basePrefix/gyms/$gymId/settings/testing-mode';
  static String kioskPin(String gymId) => '$basePrefix/gyms/$gymId/kiosk-pin';
  static String kioskPinVerify(String gymId) =>
      '$basePrefix/gyms/$gymId/kiosk-pin/verify';

  // Check-in — BE requires gymId in path: /api/v1/gyms/{gymId}/check-in
  static String checkIn(String gymId) => '$basePrefix/gyms/$gymId/check-in';
  static String checkInHistory(String gymId) =>
      '$basePrefix/gyms/$gymId/check-ins';

  @Deprecated('Use checkIn(gymId). BE requires gymId in URL path.')
  static const String checkInQr = '$basePrefix/gyms/check-in/qr';
  @Deprecated('Use checkIn(gymId). BE requires gymId in URL path.')
  static const String checkInNumber = '$basePrefix/gyms/check-in/number';

  // Classes — BE: /api/v1/gyms/{gymId}/classes[/{classId}[/book | /bookings/{bookingId}]]
  static String gymClasses(String gymId) => '$basePrefix/gyms/$gymId/classes';
  static String gymClassById(String gymId, String classId) =>
      '$basePrefix/gyms/$gymId/classes/$classId';
  static String bookGymClass(String gymId, String classId) =>
      '$basePrefix/gyms/$gymId/classes/$classId/book';
  static String cancelGymClassBooking(
          String gymId, String classId, String bookingId) =>
      '$basePrefix/gyms/$gymId/classes/$classId/bookings/$bookingId';
  static String gymClassWaitlist(String gymId, String classId) =>
      '$basePrefix/gyms/$gymId/classes/$classId/waitlist';
  static String gymClassAttendance(String gymId, String classId) =>
      '$basePrefix/gyms/$gymId/classes/$classId/attendance';

  @Deprecated('Use gymClasses(gymId). BE requires gymId in URL path.')
  static const String classes = '$basePrefix/gyms/classes';
  @Deprecated('Use gymClassById(gymId, classId). BE requires gymId in URL path.')
  static String classById(String id) => '$basePrefix/gyms/classes/$id';
  @Deprecated('Use bookGymClass/cancelGymClassBooking.')
  static String classBookings(String id) =>
      '$basePrefix/gyms/classes/$id/bookings';
  @Deprecated('Use gymClassWaitlist(gymId, classId).')
  static String classWaitlist(String id) =>
      '$basePrefix/gyms/classes/$id/waitlist';
  @Deprecated('Use gymClassAttendance(gymId, classId).')
  static String classAttendance(String id) =>
      '$basePrefix/gyms/classes/$id/attendance';

  // Announcements — BE requires gymId in the URL path.
  static String gymAnnouncements(String gymId) =>
      '$basePrefix/gyms/$gymId/announcements';
  static String gymAnnouncementById(String gymId, String id) =>
      '$basePrefix/gyms/$gymId/announcements/$id';

  @Deprecated('BE requires gymId in path. Use gymAnnouncements(gymId).')
  static const String announcements = '$basePrefix/gyms/announcements';
  @Deprecated('BE requires gymId in path. Use gymAnnouncementById(gymId, id).')
  static String announcementById(String id) =>
      '$basePrefix/gyms/announcements/$id';

  // Training
  static const String exercises = '$basePrefix/training/exercises';
  static String exerciseById(String id) => '$basePrefix/training/exercises/$id';
  static const String customExercises =
      '$basePrefix/training/exercises/custom';
  static const String workouts = '$basePrefix/training/workouts';
  static String workoutById(String id) => '$basePrefix/training/workouts/$id';
  static const String workoutTemplates = '$basePrefix/training/templates';
  static const String programs = '$basePrefix/training/programs';
  static String programById(String id) => '$basePrefix/training/programs/$id';
  static const String workoutLogs = '$basePrefix/training/workout-logs';
  static String workoutLogById(String id) =>
      '$basePrefix/training/workout-logs/$id';
  // BE route is /training/assignments/workout (POST to assign).
  // GET for the trainee's own assignments is at /training/workout-assignments.
  static const String assignWorkout =
      '$basePrefix/training/assignments/workout';
  static const String workoutAssignments =
      '$basePrefix/training/workout-assignments';
  static String workoutAssignmentById(String id) =>
      '$basePrefix/training/workout-assignments/$id';
  static const String todaysWorkout = '$basePrefix/training/todays-workout';
  static const String remoteWorkoutActivate =
      '$basePrefix/training/remote-workout/activate';
  static const String remoteWorkoutDeactivate =
      '$basePrefix/training/remote-workout/deactivate';
  static const String remoteWorkoutStatus =
      '$basePrefix/training/remote-workout/status';
  static String remoteWorkoutStatusForClient(String clientId) =>
      '$basePrefix/training/remote-workout/status/$clientId';

  // Progress
  static const String bodyMetrics = '$basePrefix/progress/body-metrics';
  static const String progressPhotos = '$basePrefix/progress/photos';
  static String progressPhotoById(String id) =>
      '$basePrefix/progress/photos/$id';
  static const String progressDashboard = '$basePrefix/progress/dashboard';
  static const String streaks = '$basePrefix/progress/streaks';
  static String clientProgress(String clientId) =>
      '$basePrefix/progress/clients/$clientId';

  // Scheduling
  static const String sessions = '$basePrefix/scheduling/sessions';
  static String sessionById(String id) =>
      '$basePrefix/scheduling/sessions/$id';
  static String sessionConfirm(String id) =>
      '$basePrefix/scheduling/sessions/$id/confirm';
  static String sessionStart(String id) =>
      '$basePrefix/scheduling/sessions/$id/start';
  static String sessionComplete(String id) =>
      '$basePrefix/scheduling/sessions/$id/complete';
  static String sessionTrainerLog(String id) =>
      '$basePrefix/scheduling/sessions/$id/trainer-log';
  static String sessionMemberLog(String id) =>
      '$basePrefix/scheduling/sessions/$id/member-log';
  static String sessionExerciseLogs(String sessionId) =>
      '$basePrefix/scheduling/sessions/$sessionId/exercise-logs';
  static String sessionExerciseLogById(String sessionId, String logId) =>
      '$basePrefix/scheduling/sessions/$sessionId/exercise-logs/$logId';
  static const String upcomingSessions =
      '$basePrefix/scheduling/sessions/upcoming';
  static const String trainerAvailability =
      '$basePrefix/scheduling/availability';
  // BE exposes bookable slots at /scheduling/available-slots (NOT /availability/slots).
  static const String availableSlots =
      '$basePrefix/scheduling/available-slots';
  static const String weeklyAvailability =
      '$basePrefix/scheduling/availability/week';
  static const String blockDate =
      '$basePrefix/scheduling/availability/block-date';
  static const String unblockDate =
      '$basePrefix/scheduling/availability/unblock-date';
  static const String recurringSessions =
      '$basePrefix/scheduling/sessions/recurring';
  static const String sessionStats =
      '$basePrefix/scheduling/sessions/stats';

  @Deprecated('BE has no /availability/slots. Use availableSlots (= /scheduling/available-slots) which requires trainerId, gymId, date.')
  static const String availabilitySlots =
      '$basePrefix/scheduling/available-slots';

  @Deprecated('BE has no GET /availability/blocked-dates. Fetch GET /scheduling/availability and read blockedDates from the response.')
  static const String blockedDates =
      '$basePrefix/scheduling/availability';

  @Deprecated('BE has no /scheduling/booking-requests. Use sessions with status=pending filter.')
  static const String bookingRequests =
      '$basePrefix/scheduling/sessions';
  @Deprecated('BE has no /scheduling/booking-requests/{id}. Use sessionById.')
  static String bookingRequestById(String id) =>
      '$basePrefix/scheduling/sessions/$id';

  // Nutrition
  static const String foodLog = '$basePrefix/nutrition/food-log';
  static const String waterLog = '$basePrefix/nutrition/water-log';
  static const String waterLogTarget = '$basePrefix/nutrition/water-log/target';
  static const String foodLogSummary = '$basePrefix/nutrition/food-log/summary';
  static const String mealPlans = '$basePrefix/nutrition/meal-plans';
  static String mealPlanById(String id) =>
      '$basePrefix/nutrition/meal-plans/$id';
  static String assignMealPlan(String mealPlanId) =>
      '$basePrefix/nutrition/meal-plans/$mealPlanId/assign';
  // Trainee's own currently-assigned meal plan (PRD /me endpoints).
  static const String myMealPlan = '$basePrefix/nutrition/me/meal-plan';
  // BE name is /food-database (search the Egyptian food DB).
  static const String foodDatabase = '$basePrefix/nutrition/food-database';
  static String clientFoodLog(String clientId) =>
      '$basePrefix/nutrition/clients/$clientId/food-log';

  @Deprecated('BE uses /nutrition/food-database. Use foodDatabase.')
  static const String foodItems = '$basePrefix/nutrition/food-database';
  @Deprecated('No BE endpoint at /nutrition/food-items/{id}.')
  static String foodItemById(String id) =>
      '$basePrefix/nutrition/food-database/$id';
  @Deprecated('BE exposes /nutrition/clients/{id}/food-log. Use clientFoodLog.')
  static String clientNutrition(String clientId) =>
      '$basePrefix/nutrition/clients/$clientId/food-log';

  // Notifications (cross-cutting; lives under top-level /notifications)
  static const String deviceTokens = '$basePrefix/notifications/device-tokens';

  // Communication
  static const String conversations = '$basePrefix/communication/conversations';
  static String conversationById(String id) =>
      '$basePrefix/communication/conversations/$id';
  static String messages(String conversationId) =>
      '$basePrefix/communication/conversations/$conversationId/messages';
  static const String notifications = '$basePrefix/communication/notifications';
  static const String notificationsUnreadCount =
      '$basePrefix/communication/notifications/unread-count';
  static const String notificationsReadAll =
      '$basePrefix/communication/notifications/read-all';
  static String notificationMarkRead(String id) =>
      '$basePrefix/communication/notifications/$id/read';
  @Deprecated('BE has no /communication/notifications/{id}. Use notificationMarkRead(id) for PUT mark-as-read.')
  static String notificationById(String id) =>
      '$basePrefix/communication/notifications/$id';
  static const String notificationPreferences =
      '$basePrefix/communication/notification-preferences';
  static const String broadcast = '$basePrefix/communication/broadcast';

  // Billing
  static String trainerEarnings(String trainerId) =>
      '$basePrefix/billing/trainers/$trainerId/earnings';
  static const String payments = '$basePrefix/billing/payments';
  static String paymentById(String id) => '$basePrefix/billing/payments/$id';
  static String voidPayment(String paymentId) =>
      '$basePrefix/billing/payments/$paymentId/void';
  // BE path is /billing/plans (NOT /billing/membership-plans).
  static const String membershipPlans = '$basePrefix/billing/plans';
  // BE path is /billing/packages (NOT /billing/session-packages).
  static const String sessionPackages = '$basePrefix/billing/packages';
  static const String packageTemplates =
      '$basePrefix/billing/package-templates';
  static String packageTemplateById(String id) =>
      '$basePrefix/billing/package-templates/$id';
  // BE path is /billing/packages/assign.
  static const String assignPackage = '$basePrefix/billing/packages/assign';
  static const String planFeatures = '$basePrefix/billing/plan-features';
  // Trainer's view of all their clients' active packages.
  static const String myClientPackages =
      '$basePrefix/billing/packages/my-clients';
  static String memberBalance(String memberId) =>
      '$basePrefix/billing/members/$memberId/balance';
  static String billingMemberPayments(String memberId) =>
      '$basePrefix/billing/members/$memberId/payments';
  static const String revenue = '$basePrefix/billing/revenue';
  static const String revenueByTrainer = '$basePrefix/billing/revenue/by-trainer';
  // Trainee's own packages and payments (PRD /me endpoints).
  static const String myPackages = '$basePrefix/billing/me/packages';
  static const String myPayments = '$basePrefix/billing/me/payments';

  @Deprecated('BE has no /billing/session-packages/{id}. Use sessionPackages with filters.')
  static String sessionPackageById(String id) =>
      '$basePrefix/billing/packages/$id';
  @Deprecated('BE has no /billing/session-packages/active. Use myClientPackages (trainer scope).')
  static const String activePackages =
      '$basePrefix/billing/packages/my-clients';
  @Deprecated('BE has no /billing/session-packages/client/{clientId}. Use sessionPackages with memberId filter.')
  static String clientPackages(String clientId) =>
      '$basePrefix/billing/packages';

  // Gamification
  static const String leaderboards = '$basePrefix/gamification/leaderboards';
  static String leaderboardByType(String type) =>
      '$basePrefix/gamification/leaderboards/$type';
  static const String myLeaderboardPosition =
      '$basePrefix/gamification/leaderboards/my-position';
  static const String leaderboardOptOut =
      '$basePrefix/gamification/leaderboards/opt-out';
  static const String trainerRatings =
      '$basePrefix/gamification/trainer-ratings';
  static const String testimonialsPending =
      '$basePrefix/gamification/testimonials/pending';
  static String trainerPublicProfile(String trainerId) =>
      '$basePrefix/gamification/trainers/$trainerId/public-profile';
  static String trainerDetailedRatings(String trainerId) =>
      '$basePrefix/gamification/trainers/$trainerId/ratings';
  static String trainerTestimonials(String trainerId) =>
      '$basePrefix/gamification/trainers/$trainerId/testimonials';
  static const String trainerLeaderboard =
      '$basePrefix/gamification/trainers/leaderboard';
  static const String coachOfTheMonth =
      '$basePrefix/gamification/trainers/coach-of-the-month';

  @Deprecated('BE has no /gamification/trainers/my-ratings. Use trainerPublicProfile(trainerId) for summary, trainerDetailedRatings(trainerId) for list.')
  static const String myRatings =
      '$basePrefix/gamification/trainers/my-ratings';

  // Members (gym staff perspective) — BE requires gymId in the URL path.
  static String gymMembers(String gymId) => '$basePrefix/gyms/$gymId/members';
  static String gymMemberById(String gymId, String memberId) =>
      '$basePrefix/gyms/$gymId/members/$memberId';
  static String gymMemberPayments(String gymId, String memberId) =>
      '$basePrefix/gyms/$gymId/members/$memberId/payments';
  static String gymMemberFreeze(String gymId, String memberId) =>
      '$basePrefix/gyms/$gymId/members/$memberId/freeze';
  static String gymMemberUnfreeze(String gymId, String memberId) =>
      '$basePrefix/gyms/$gymId/members/$memberId/unfreeze';
  static String gymMemberNotes(String gymId, String memberId) =>
      '$basePrefix/gyms/$gymId/members/$memberId/notes';
  static String gymMemberOnboarding(String gymId, String memberId) =>
      '$basePrefix/gyms/$gymId/members/$memberId/onboarding';

  @Deprecated('BE requires gymId in path. Use gymMembers(gymId).')
  static const String members = '$basePrefix/gyms/members';
  @Deprecated('BE requires gymId in path. Use gymMemberById(gymId, memberId).')
  static String memberById(String id) => '$basePrefix/gyms/members/$id';
  @Deprecated('Use gymMemberPayments(gymId, memberId).')
  static String memberPayments(String id) =>
      '$basePrefix/gyms/members/$id/payments';
  @Deprecated('Use gymMemberFreeze(gymId, memberId).')
  static String freezeMember(String id) =>
      '$basePrefix/gyms/members/$id/freeze';
  @Deprecated('Use gymMemberUnfreeze(gymId, memberId).')
  static String unfreezeMember(String id) =>
      '$basePrefix/gyms/members/$id/unfreeze';

  // Trainers (gym staff perspective) — BE requires gymId in the URL path.
  static String gymTrainers(String gymId) => '$basePrefix/gyms/$gymId/trainers';
  static String gymTrainerRate(String gymId, String trainerId) =>
      '$basePrefix/gyms/$gymId/trainers/$trainerId/rate';

  @Deprecated('BE requires gymId in path. Use gymTrainers(gymId).')
  static const String trainers = '$basePrefix/gyms/trainers';
  @Deprecated('No BE endpoint at /gyms/trainers/{id}. Use gymTrainerRate for rate, gymTrainers to list.')
  static String trainerById(String id) => '$basePrefix/gyms/trainers/$id';

  // Analytics — BE requires gymId in path: /gyms/{gymId}/analytics
  static String gymAnalyticsByGym(String gymId) =>
      '$basePrefix/gyms/$gymId/analytics';

  // Pro-gated: hour × day-of-week heatmap over a configurable range.
  static String gymPeakHoursByGym(String gymId) =>
      '$basePrefix/gyms/$gymId/analytics/peak-hours';

  @Deprecated('BE requires gymId in path. Use gymAnalyticsByGym(gymId).')
  static const String gymAnalytics = '$basePrefix/gyms/analytics';
  @Deprecated('BE requires gymId in path. Use gymPeakHoursByGym(gymId).')
  static const String gymAnalyticsPeakHours =
      '$basePrefix/gyms/analytics/peak-hours';
  @Deprecated('No BE endpoint at /gyms/analytics/revenue. Use billing revenue.')
  static const String gymAnalyticsRevenue =
      '$basePrefix/gyms/analytics/revenue';
  @Deprecated('No BE endpoint at /gyms/analytics/churn.')
  static const String gymAnalyticsChurn = '$basePrefix/gyms/analytics/churn';
  @Deprecated('No BE endpoint at /gyms/analytics/retention.')
  static const String gymAnalyticsRetention =
      '$basePrefix/gyms/analytics/retention';
}
