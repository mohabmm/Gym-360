import 'package:freezed_annotation/freezed_annotation.dart';
import 'exercise_model.dart';

part 'workout_model.freezed.dart';
part 'workout_model.g.dart';

@freezed
class WorkoutModel with _$WorkoutModel {
  const factory WorkoutModel({
    required String id,
    required String name,
    String? nameAr,
    String? description,
    String? trainerId,
    String? trainerName,
    String? gymId,
    // Backend's list endpoint (GET /training/workouts) omits exercises and
    // sends `exerciseCount` instead; the detail endpoint sends the full list.
    @Default(<WorkoutExerciseModel>[])
    List<WorkoutExerciseModel> exercises,
    int? exerciseCount,
    @Default(false) bool isTemplate,
    // WorkoutDto serializes this as string[] (matches MuscleGroup enum names).
    @Default(<String>[]) List<String> targetMuscleGroups,
    int? estimatedDurationMinutes,
    DateTime? createdAt,
  }) = _WorkoutModel;

  factory WorkoutModel.fromJson(Map<String, dynamic> json) =>
      _$WorkoutModelFromJson(json);
}

@freezed
class WorkoutExerciseModel with _$WorkoutExerciseModel {
  // Backend stores `reps` as a string to support ranges like "8-12".
  // `id`/`orderIndex` are only present on detail responses; some legacy
  // payloads (older list responses, fixtures) omit them.
  const factory WorkoutExerciseModel({
    String? id,
    required String exerciseId,
    ExerciseModel? exercise,
    int? orderIndex,
    required int sets,
    String? reps,
    double? weight,
    int? durationSeconds,
    int? restSeconds,
    String? notes,
    String? supersetGroupId,
  }) = _WorkoutExerciseModel;

  factory WorkoutExerciseModel.fromJson(Map<String, dynamic> json) =>
      _$WorkoutExerciseModelFromJson(json);
}

// Mirrors `WorkoutLogDto` on the backend (Training module).
@freezed
class WorkoutLogModel with _$WorkoutLogModel {
  const factory WorkoutLogModel({
    required String id,
    required String userId,
    String? gymId,
    String? workoutId,
    String? workoutName,
    DateTime? date,
    required DateTime startedAt,
    DateTime? completedAt,
    int? durationMinutes,
    String? notes,
    @Default(<LogSetModel>[]) List<LogSetModel> sets,
    @Default(<String>[]) List<String> muscleGroupsWorked,
    int? perceivedExertion,
  }) = _WorkoutLogModel;

  factory WorkoutLogModel.fromJson(Map<String, dynamic> json) =>
      _$WorkoutLogModelFromJson(json);
}

// Mirrors `LogSetDto` on the backend. The BE does not persist per-set IDs —
// [id] is only populated client-side for local list-keying; [rpe] is carried
// per-set in the UI and averaged into [WorkoutLogModel.perceivedExertion].
@freezed
class LogSetModel with _$LogSetModel {
  const factory LogSetModel({
    String? id,
    required String exerciseId,
    String? exerciseName,
    required int setNumber,
    int? reps,
    double? weight,
    @Default('Kg') String weightUnit,
    int? durationSeconds,
    @Default(true) bool isCompleted,
    int? rpe,
    String? notes,
  }) = _LogSetModel;

  factory LogSetModel.fromJson(Map<String, dynamic> json) =>
      _$LogSetModelFromJson(json);
}

@freezed
class WorkoutAssignmentModel with _$WorkoutAssignmentModel {
  // Mirrors src/.../DTOs/WorkoutAssignmentDto.cs. The BE field is `clientId`
  // (not `traineeId`), `workoutId` is nullable (program-level assignments
  // carry only a programId), and the DTO carries no nested workout object.
  const factory WorkoutAssignmentModel({
    required String id,
    String? workoutId,
    String? programId,
    WorkoutModel? workout,
    required String clientId,
    required String trainerId,
    String? gymId,
    String? assignmentType,
    DateTime? startDate,
    DateTime? endDate,
    DateTime? assignedAt,
    DateTime? scheduledDate,
    String? status,
    String? notes,
    @Default(false) bool isRemoteMode,
  }) = _WorkoutAssignmentModel;

  factory WorkoutAssignmentModel.fromJson(Map<String, dynamic> json) =>
      _$WorkoutAssignmentModelFromJson(json);
}

@freezed
class ProgramModel with _$ProgramModel {
  const factory ProgramModel({
    required String id,
    required String name,
    String? nameAr,
    String? description,
    required String trainerId,
    required int durationWeeks,
    @Default(<ProgramDayModel>[]) List<ProgramDayModel> days,
    DateTime? createdAt,
  }) = _ProgramModel;

  factory ProgramModel.fromJson(Map<String, dynamic> json) =>
      _$ProgramModelFromJson(json);
}

@freezed
class ProgramDayModel with _$ProgramDayModel {
  const factory ProgramDayModel({
    required String id,
    required int weekNumber,
    required int dayOfWeek,
    required String workoutId,
    WorkoutModel? workout,
    String? notes,
  }) = _ProgramDayModel;

  factory ProgramDayModel.fromJson(Map<String, dynamic> json) =>
      _$ProgramDayModelFromJson(json);
}
