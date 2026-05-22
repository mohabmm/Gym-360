import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/progress_remote_datasource.dart';

class ProgressRepository {
  final ProgressRemoteDataSource _remoteDataSource;

  ProgressRepository({required ProgressRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  /// Dashboard includes streaks — no separate streak endpoint on BE.
  Future<ProgressDashboardModel> getDashboard({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getDashboard(userId: userId, gymId: gymId);

  Future<List<BodyMetricEntryModel>> getBodyMetrics({
    required String userId,
    required String gymId,
    String? metricType,
    DateTime? from,
    DateTime? to,
  }) =>
      _remoteDataSource.getBodyMetrics(
        userId: userId,
        gymId: gymId,
        metricType: metricType,
        from: from,
        to: to,
      );

  /// Returns the metric ID (Guid string).
  Future<String> logBodyMetric(Map<String, dynamic> body) =>
      _remoteDataSource.logBodyMetric(body);

  Future<List<ProgressPhotoSetModel>> getProgressPhotos({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getProgressPhotos(userId: userId, gymId: gymId);

  /// Returns the photo ID (Guid string).
  Future<String> uploadProgressPhoto(Map<String, dynamic> body) =>
      _remoteDataSource.uploadProgressPhoto(body);

  Future<void> deleteProgressPhoto(
    String id, {
    required String userId,
  }) =>
      _remoteDataSource.deleteProgressPhoto(id, userId: userId);
}
