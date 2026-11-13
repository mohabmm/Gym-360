import 'package:gymapp_core/gymapp_core.dart';

/// pt.progress_measurements: the member's progress since day one and their goal.
/// The member comes from the JWT; only the gym is sent.
class ProgressJourneyApi {
  final ApiClient _apiClient;

  ProgressJourneyApi({required ApiClient apiClient}) : _apiClient = apiClient;

  /// Every entry since day one (oldest first) + goal.
  Future<ProgressJourneyModel> getJourney(GymId gymId) => _apiClient.get<ProgressJourneyModel>(
        ApiEndpoints.progressJourney,
        queryParameters: {'gymId': gymId},
        parser: (data) => ProgressJourneyModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );

  /// Sets the member's own goal and optional target weight (kg).
  Future<void> setGoal(GymId gymId, GoalSelection selection) => _apiClient.put<Object?>(
        ApiEndpoints.progressGoal,
        queryParameters: {'gymId': gymId},
        data: {'goal': selection.goal.apiValue, 'targetWeight': selection.targetWeight},
        parser: (data) => data,
      );
}
