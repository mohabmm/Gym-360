import 'package:gymapp_core/gymapp_core.dart';

/// Returns the inner list whether the endpoint responds with a bare JSON
/// array or a paged envelope `{ items, page, pageSize, totalCount, totalPages }`.
List<dynamic> _unwrapItems(dynamic data) {
  if (data is List) return data;
  if (data is Map<String, dynamic> && data['items'] is List) {
    return data['items'] as List<dynamic>;
  }
  return const [];
}

class WorkoutsRemoteDataSource {
  final ApiClient _apiClient;

  WorkoutsRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  Future<List<WorkoutModel>> getWorkouts() async {
    return _apiClient.get<List<WorkoutModel>>(
      ApiEndpoints.workouts,
      parser: (data) => _unwrapItems(data)
          .map<WorkoutModel>((e) => WorkoutModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<WorkoutModel> getWorkoutById(String id) async {
    return _apiClient.get<WorkoutModel>(
      ApiEndpoints.workoutById(WorkoutId(id)),
      parser: (data) => WorkoutModel.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<WorkoutModel>> getWorkoutTemplates() async {
    return _apiClient.get<List<WorkoutModel>>(
      ApiEndpoints.workoutTemplates,
      parser: (data) => _unwrapItems(data)
          .map<WorkoutModel>((e) => WorkoutModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<WorkoutLogModel>> getWorkoutLogs() async {
    return _apiClient.get<List<WorkoutLogModel>>(
      ApiEndpoints.workoutLogs,
      parser: (data) => _unwrapItems(data)
          .map((e) => _workoutLogFromDto(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Backend POST /training/workout-logs returns the created log's `Guid`
  /// (bare string), not the full log object. Clients don't need the full log
  /// here — they refresh the list right after — so we simply return the id.
  Future<String> logWorkout(Map<String, dynamic> body) async {
    return _apiClient.post<String>(
      ApiEndpoints.workoutLogs,
      data: body,
      parser: (data) {
        if (data is String) return data;
        if (data is Map<String, dynamic> && data['id'] != null) {
          return data['id'].toString();
        }
        return data.toString();
      },
    );
  }

  Future<List<ExerciseModel>> getExercises({String? query, String? muscleGroup}) async {
    return _apiClient.get<List<ExerciseModel>>(
      ApiEndpoints.exercises,
      queryParameters: {
        'page': 1,
        'pageSize': 100,
        if (query != null) 'search': query,
        if (muscleGroup != null) 'muscleGroup': muscleGroup,
      },
      parser: (data) => _unwrapItems(data)
          .map<ExerciseModel>((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<WorkoutAssignmentModel>> getWorkoutAssignments() async {
    return _apiClient.get<List<WorkoutAssignmentModel>>(
      ApiEndpoints.workoutAssignments,
      parser: (data) => _unwrapItems(data)
          .map<WorkoutAssignmentModel>((e) => WorkoutAssignmentModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// Adapts the backend `WorkoutLogDto` shape to [WorkoutLogModel]. The DTO
/// ships `date` as a `DateOnly` string ("YYYY-MM-DD") and aggregate fields
/// (`perceivedExertion`, `muscleGroupsWorked`) that don't fit into individual
/// sets; set-level `id`/`rpe` are never returned by the BE.
WorkoutLogModel _workoutLogFromDto(Map<String, dynamic> dto) {
  final startedAtRaw = dto['startedAt'] ?? dto['date'];
  final startedAt = startedAtRaw is String
      ? DateTime.parse(startedAtRaw)
      : DateTime.now();

  DateTime? parseDate(dynamic v) =>
      v is String ? DateTime.tryParse(v) : null;

  final rawSets = dto['sets'];
  final sets = rawSets is List
      ? rawSets
          .map<LogSetModel>((s) => _logSetFromDto(s as Map<String, dynamic>))
          .toList()
      : <LogSetModel>[];

  final rawMuscles = dto['muscleGroupsWorked'];
  final muscles = rawMuscles is List
      ? rawMuscles.map<String>((m) => m.toString()).toList()
      : const <String>[];

  return WorkoutLogModel(
    id: dto['id'].toString(),
    userId: (dto['userId'] ?? '').toString(),
    gymId: dto['gymId']?.toString(),
    workoutId: dto['workoutId']?.toString(),
    workoutName: dto['workoutName'] as String?,
    date: parseDate(dto['date']),
    startedAt: startedAt,
    completedAt: parseDate(dto['completedAt']),
    durationMinutes: (dto['durationMinutes'] as num?)?.toInt(),
    notes: dto['notes'] as String?,
    sets: sets,
    muscleGroupsWorked: muscles,
    perceivedExertion: (dto['perceivedExertion'] as num?)?.toInt(),
  );
}

LogSetModel _logSetFromDto(Map<String, dynamic> dto) {
  return LogSetModel(
    id: dto['id']?.toString(),
    exerciseId: (dto['exerciseId'] ?? '').toString(),
    exerciseName: dto['exerciseName'] as String?,
    setNumber: (dto['setNumber'] as num?)?.toInt() ?? 0,
    reps: (dto['reps'] as num?)?.toInt(),
    weight: (dto['weight'] as num?)?.toDouble(),
    weightUnit: (dto['weightUnit'] ?? 'Kg').toString(),
    durationSeconds: (dto['durationSeconds'] as num?)?.toInt(),
    isCompleted: dto['isCompleted'] is bool ? dto['isCompleted'] as bool : true,
    notes: dto['notes'] as String?,
  );
}
