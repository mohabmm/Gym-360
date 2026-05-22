import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/workouts_remote_datasource.dart';

class WorkoutsRepository {
  final WorkoutsRemoteDataSource _remoteDataSource;

  WorkoutsRepository({required WorkoutsRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<List<WorkoutModel>> getWorkouts() => _remoteDataSource.getWorkouts();

  Future<WorkoutModel> getWorkoutById(String id) =>
      _remoteDataSource.getWorkoutById(id);

  Future<List<WorkoutModel>> getWorkoutTemplates() =>
      _remoteDataSource.getWorkoutTemplates();

  Future<List<WorkoutLogModel>> getWorkoutLogs() =>
      _remoteDataSource.getWorkoutLogs();

  Future<String> logWorkout(Map<String, dynamic> body) =>
      _remoteDataSource.logWorkout(body);

  Future<List<ExerciseModel>> getExercises({String? query, String? muscleGroup}) =>
      _remoteDataSource.getExercises(query: query, muscleGroup: muscleGroup);

  Future<List<WorkoutAssignmentModel>> getWorkoutAssignments() =>
      _remoteDataSource.getWorkoutAssignments();
}
