import 'package:gymapp_core/gymapp_core.dart';

class GamificationRemoteDataSource {
  final ApiClient _apiClient;

  GamificationRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE GET /gamification/leaderboards/{type} requires gymId query param.
  Future<LeaderboardModel> getLeaderboard(
    String type, {
    required String gymId,
  }) async {
    return _apiClient.get<LeaderboardModel>(
      ApiEndpoints.leaderboardByType(LeaderboardType(type)),
      queryParameters: {'gymId': gymId},
      parser: (data) => LeaderboardModel.fromJson(data as Map<String, dynamic>),
    );
  }

  /// BE GET /gamification/trainers/{trainerId}/public-profile requires gymId.
  Future<TrainerPublicProfileModel> getTrainerPublicProfile(
    String trainerId, {
    required String gymId,
  }) async {
    return _apiClient.get<TrainerPublicProfileModel>(
      ApiEndpoints.trainerPublicProfile(TrainerId(trainerId)),
      queryParameters: {'gymId': gymId},
      parser: (data) =>
          TrainerPublicProfileModel.fromJson(data as Map<String, dynamic>),
    );
  }

  /// BE POST /gamification/trainer-ratings returns Guid (rating ID), 201.
  Future<String> rateTrainer(RateTrainerRequest request) async {
    return _apiClient.post<String>(
      ApiEndpoints.trainerRatings,
      data: request.toJson(),
      parser: (data) => data.toString(),
    );
  }

  /// BE PUT /gamification/leaderboards/opt-out is a toggle.
  /// userId is derived from the JWT; only gymId is required in the body.
  Future<void> toggleLeaderboardOptOut({
    required String gymId,
  }) async {
    await _apiClient.putVoid(
      ApiEndpoints.leaderboardOptOut,
      data: {
        'gymId': gymId,
      },
    );
  }
}
