import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/home_remote_datasource.dart';

class HomeRepository {
  final HomeRemoteDataSource _remoteDataSource;

  HomeRepository({required HomeRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<WorkoutModel?> getTodaysWorkout({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getTodaysWorkout(userId: userId, gymId: gymId);

  Future<List<SessionModel>> getUpcomingSessions({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getUpcomingSessions(userId: userId, gymId: gymId);

  /// Dashboard includes streaks — no separate streak endpoint on BE.
  Future<ProgressDashboardModel> getProgressDashboard({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getProgressDashboard(userId: userId, gymId: gymId);

  Future<WaterLogModel> getTodaysWater({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getTodaysWater(userId: userId, gymId: gymId);
}
