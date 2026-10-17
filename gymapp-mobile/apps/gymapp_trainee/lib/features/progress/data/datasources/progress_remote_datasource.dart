import 'package:gymapp_core/gymapp_core.dart';

import '../models/body_metrics_query.dart';

export '../models/body_metrics_query.dart';

class ProgressRemoteDataSource {
  final ApiClient _apiClient;

  ProgressRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE GET /progress/dashboard requires userId, gymId.
  /// Response includes streaks — no separate /progress/streaks endpoint exists.
  Future<ProgressDashboardModel> getDashboard({
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

  /// BE GET /progress/body-metrics requires userId, gymId.
  Future<List<BodyMetricEntryModel>> getBodyMetrics(
      BodyMetricsQuery query) async {
    return _apiClient.get<List<BodyMetricEntryModel>>(
      ApiEndpoints.bodyMetrics,
      queryParameters: query.toQueryParameters(),
      parser: (data) => (data as List<dynamic>)
          .map<BodyMetricEntryModel>((e) => BodyMetricEntryModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// BE POST /progress/body-metrics returns Guid (metric ID), 201.
  Future<String> logBodyMetric(Map<String, dynamic> body) async {
    return _apiClient.post<String>(
      ApiEndpoints.bodyMetrics,
      data: body,
      parser: (data) => data.toString(),
    );
  }

  /// BE GET /progress/photos requires userId, gymId, page, pageSize.
  Future<List<ProgressPhotoSetModel>> getProgressPhotos({
    required String userId,
    required String gymId,
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<ProgressPhotoSetModel>>(
      ApiEndpoints.progressPhotos,
      queryParameters: {
        'userId': userId,
        'gymId': gymId,
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
          return <ProgressPhotoSetModel>[];
        }
        return list
            .map(
                (e) => ProgressPhotoSetModel.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  }

  /// BE POST /progress/photos returns Guid (photo ID), 201.
  Future<String> uploadProgressPhoto(Map<String, dynamic> body) async {
    return _apiClient.post<String>(
      ApiEndpoints.progressPhotos,
      data: body,
      parser: (data) => data.toString(),
    );
  }

  /// BE DELETE /progress/photos/{photoId} requires userId query param.
  Future<void> deleteProgressPhoto(
    String id, {
    required String userId,
  }) async {
    await _apiClient.delete(
      ApiEndpoints.progressPhotoById(id),
      queryParameters: {'userId': userId},
    );
  }
}
