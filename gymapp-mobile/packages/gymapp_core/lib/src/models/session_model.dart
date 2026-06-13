import 'package:freezed_annotation/freezed_annotation.dart';

part 'session_model.freezed.dart';
part 'session_model.g.dart';

// Backend returns either the DTO shape (`scheduledAt`) or the entity shape
// (`scheduledDate` + `scheduledTime`) depending on build. Normalize here so
// the generated parser always sees `scheduledAt`.
Map<String, dynamic> _normalizeScheduledAt(Map<String, dynamic> json) {
  if (json['scheduledAt'] != null) return json;
  final date = json['scheduledDate'];
  final time = json['scheduledTime'];
  if (date is String && time is String) {
    return {...json, 'scheduledAt': '${date}T$time'};
  }
  return json;
}

/// Matches the backend SessionDto shape (scheduling.SessionDto) returned by
/// GET /api/v1/scheduling/sessions[/...]. See docs/01_GYMAPP_BACKEND_COMPLETE.md MODULE 5.
@freezed
class SessionModel with _$SessionModel {
  const factory SessionModel({
    required String id,
    required String gymId,
    required String trainerId,
    required String memberId,
    required DateTime scheduledAt,
    required int durationMinutes,
    required String sessionType,
    String? focus,
    required String status,
    @Default(false) bool isRecurring,
    required DateTime createdAt,
  }) = _SessionModel;

  factory SessionModel.fromJson(Map<String, dynamic> json) =>
      _$SessionModelFromJson(_normalizeScheduledAt(json));
}

@freezed
class SessionTrainerLogModel with _$SessionTrainerLogModel {
  const factory SessionTrainerLogModel({
    required String id,
    String? exercisesPerformed,
    String? actualVsPlanned,
    int? clientEffortRating,
    String? feedback,
    String? techniqueNotes,
    String? areasToImprove,
    String? nextSessionFocus,
    @Default(false) bool attendanceConfirmed,
    DateTime? loggedAt,
  }) = _SessionTrainerLogModel;

  factory SessionTrainerLogModel.fromJson(Map<String, dynamic> json) =>
      _$SessionTrainerLogModelFromJson(json);
}

@freezed
class SessionMemberLogModel with _$SessionMemberLogModel {
  const factory SessionMemberLogModel({
    required String id,
    int? rating,
    int? perceivedExertion,
    String? personalNotes,
    String? discomfort,
    int? satisfaction,
    DateTime? loggedAt,
  }) = _SessionMemberLogModel;

  factory SessionMemberLogModel.fromJson(Map<String, dynamic> json) =>
      _$SessionMemberLogModelFromJson(json);
}

/// Matches the backend SessionExerciseLogDto — per-exercise actuals trainer
/// records after a completed session. Trainee can view on session detail.
@freezed
class SessionExerciseLogModel with _$SessionExerciseLogModel {
  const factory SessionExerciseLogModel({
    required String id,
    required String sessionId,
    String? exerciseId,
    required String exerciseName,
    String? exerciseNameAr,
    required int orderIndex,
    required int sets,
    required String reps,
    double? weight,
    String? notes,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _SessionExerciseLogModel;

  factory SessionExerciseLogModel.fromJson(Map<String, dynamic> json) =>
      _$SessionExerciseLogModelFromJson(json);
}

/// Matches the backend SessionDetailDto — superset of SessionDto with logs + cancellation.
@freezed
class SessionDetailModel with _$SessionDetailModel {
  const factory SessionDetailModel({
    required String id,
    required String gymId,
    required String trainerId,
    required String memberId,
    required DateTime scheduledAt,
    required int durationMinutes,
    required String sessionType,
    String? focus,
    required String status,
    @Default(false) bool isRecurring,
    String? recurrenceRule,
    String? cancellationReason,
    DateTime? cancelledAt,
    required DateTime createdAt,
    SessionTrainerLogModel? trainerLog,
    SessionMemberLogModel? memberLog,
    @Default([]) List<SessionExerciseLogModel> exerciseLogs,
  }) = _SessionDetailModel;

  factory SessionDetailModel.fromJson(Map<String, dynamic> json) =>
      _$SessionDetailModelFromJson(_normalizeScheduledAt(json));
}

/// POST /api/v1/scheduling/sessions body. `gymId` and `memberId` are derived
/// server-side from the JWT + X-Gym-Id header — do NOT send them.
@freezed
class BookSessionRequest with _$BookSessionRequest {
  const factory BookSessionRequest({
    required String trainerId,
    required String date,
    required String time,
    required int durationMinutes,
    required String sessionType,
    String? focus,
  }) = _BookSessionRequest;

  factory BookSessionRequest.fromJson(Map<String, dynamic> json) =>
      _$BookSessionRequestFromJson(json);
}
