/// Typed identifiers for the backend entities addressed in URL paths.
///
/// Each id is a zero-cost Dart extension type over the wire `String` (a GUID
/// on the backend), so JSON and HTTP behaviour are unchanged. Because every id
/// `implements String` it can still be interpolated, compared or passed where
/// a plain `String` is expected — but a raw `String` (or an id of a different
/// entity) cannot be passed where, say, a [GymId] is required. That turns
/// "member id passed as gym id" into a compile error at the endpoint builders.
library;

// Tenancy / people
extension type const GymId(String value) implements String {}

/// The authenticated principal's identity user id.
extension type const UserId(String value) implements String {}

/// A gym member, as addressed by the gym-staff routes (`/members/{memberId}`).
extension type const MemberId(String value) implements String {}

/// A trainer, as addressed by `/trainers/{trainerId}` routes.
extension type const TrainerId(String value) implements String {}

/// A trainer's client (a trainee), as addressed by `/clients/{clientId}`.
extension type const ClientId(String value) implements String {}

// Gyms
extension type const ClassId(String value) implements String {}
extension type const BookingId(String value) implements String {}
extension type const AnnouncementId(String value) implements String {}

// Training
extension type const ExerciseId(String value) implements String {}
extension type const WorkoutId(String value) implements String {}
extension type const ProgramId(String value) implements String {}
extension type const WorkoutLogId(String value) implements String {}
extension type const WorkoutAssignmentId(String value) implements String {}

// Progress
extension type const ProgressPhotoId(String value) implements String {}

// Scheduling
extension type const SessionId(String value) implements String {}
extension type const ExerciseLogId(String value) implements String {}

// Nutrition
extension type const MealPlanId(String value) implements String {}
extension type const FoodItemId(String value) implements String {}

// Communication
extension type const ConversationId(String value) implements String {}
extension type const NotificationId(String value) implements String {}

// Billing
extension type const PaymentId(String value) implements String {}
extension type const PackageTemplateId(String value) implements String {}
extension type const SessionPackageId(String value) implements String {}

// Gamification
/// Leaderboard kind in `/gamification/leaderboards/{type}` (e.g. `attendance`,
/// `workouts_per_month`).
extension type const LeaderboardType(String value) implements String {}
