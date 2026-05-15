import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/gamification_remote_datasource.dart';

class GamificationRepository {
  final GamificationRemoteDataSource _remoteDataSource;

  GamificationRepository(
      {required GamificationRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<LeaderboardModel> getLeaderboard(
    String type, {
    required String gymId,
  }) =>
      _remoteDataSource.getLeaderboard(type, gymId: gymId);

  Future<TrainerPublicProfileModel> getTrainerPublicProfile(
    String trainerId, {
    required String gymId,
  }) =>
      _remoteDataSource.getTrainerPublicProfile(trainerId, gymId: gymId);

  /// Returns the rating ID (Guid string).
  Future<String> rateTrainer(RateTrainerRequest request) =>
      _remoteDataSource.rateTrainer(request);

  /// Toggles leaderboard opt-out for the authenticated user at the given gym.
  /// userId is derived from JWT.
  Future<void> toggleLeaderboardOptOut({
    required String gymId,
  }) =>
      _remoteDataSource.toggleLeaderboardOptOut(gymId: gymId);
}
