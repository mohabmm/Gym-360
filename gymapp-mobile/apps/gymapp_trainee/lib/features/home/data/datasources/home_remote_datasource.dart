import 'package:gymapp_core/gymapp_core.dart';

class HomeRemoteDataSource {
  final ApiClient _apiClient;

  HomeRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// Returns today's assigned workout if remote mode is active, else null.
  Future<WorkoutModel?> getTodaysWorkout({
    required String userId,
    required String gymId,
  }) async {
    try {
      return await _apiClient.get<WorkoutModel?>(
        ApiEndpoints.todaysWorkout,
        queryParameters: {'userId': userId, 'gymId': gymId},
        parser: (data) {
          if (data == null) return null;
          final json = data as Map<String, dynamic>;
          // BE returns TodaysWorkoutResponse (assignmentId, workoutId,
          // workoutName, workoutDescription, ...). Map onto WorkoutModel shape.
          return WorkoutModel.fromJson({
            'id': json['workoutId'],
            'name': json['workoutName'],
            'description': json['workoutDescription'],
            'estimatedDurationMinutes': json['estimatedDurationMinutes'],
            'targetMuscleGroups': json['targetMuscleGroups'] ?? <String>[],
          });
        },
      );
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  /// BE GET /scheduling/sessions/upcoming requires userId, gymId, role.
  Future<List<SessionModel>> getUpcomingSessions({
    required String userId,
    required String gymId,
  }) async {
    return _apiClient.get<List<SessionModel>>(
      ApiEndpoints.upcomingSessions,
      queryParameters: {
        'userId': userId,
        'gymId': gymId,
        'role': 'member',
      },
      parser: (data) => (data as List<dynamic>)
          .map<SessionModel>((e) => SessionModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// BE GET /progress/dashboard requires userId, gymId.
  Future<ProgressDashboardModel> getProgressDashboard({
    required String userId,
    required String gymId,
  }) async {
    return _apiClient.get<ProgressDashboardModel>(
      ApiEndpoints.progressDashboard,
      queryParameters: {
        'userId': userId,
        'gymId': gymId,
      },
      parser: (data) =>
          ProgressDashboardModel.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<WaterLogModel> getTodaysWater({
    required String userId,
    required String gymId,
  }) async {
    return _apiClient.get<WaterLogModel>(
      ApiEndpoints.waterLog,
      queryParameters: {
        'userId': userId,
        'gymId': gymId,
        'date': DateTime.now().toIso8601String().split('T')[0],
      },
      parser: (data) => WaterLogModel.fromJson(data as Map<String, dynamic>),
    );
  }
}
