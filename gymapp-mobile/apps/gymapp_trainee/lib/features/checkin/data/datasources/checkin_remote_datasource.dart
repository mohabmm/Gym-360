import 'package:gymapp_core/gymapp_core.dart';

class CheckInRemoteDataSource {
  final ApiClient _apiClient;

  CheckInRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE GET /gyms/{gymId}/check-ins returns PagedResponse<CheckInDto>.
  /// ApiClient.get unwraps the `data` list from the envelope.
  Future<List<CheckInModel>> getCheckInHistory(
    String gymId, {
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<CheckInModel>>(
      ApiEndpoints.checkInHistory(gymId),
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) {
        final List<dynamic> list;
        if (data is List) {
          list = data;
        } else if (data is Map<String, dynamic> && data['items'] is List) {
          list = data['items'] as List<dynamic>;
        } else {
          return <CheckInModel>[];
        }
        return list
            .map<CheckInModel>((e) => CheckInModel.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  }
}
