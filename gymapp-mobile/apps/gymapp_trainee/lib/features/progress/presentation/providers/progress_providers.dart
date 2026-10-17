import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/progress_remote_datasource.dart';
import '../../data/repositories/progress_repository.dart';

final progressRemoteDataSourceProvider =
    Provider<ProgressRemoteDataSource>((ref) {
  return ProgressRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final progressRepositoryProvider = Provider<ProgressRepository>((ref) {
  return ProgressRepository(
    remoteDataSource: ref.watch(progressRemoteDataSourceProvider),
  );
});

/// Dashboard includes streaks — no separate /progress/streaks endpoint on BE.
final progressDashboardDetailProvider =
    FutureProvider<ProgressDashboardModel>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) {
    throw Exception('No authenticated user or active gym');
  }
  final repo = ref.watch(progressRepositoryProvider);
  return repo.getDashboard(userId: userId, gymId: gymId);
});

final selectedMetricTypeProvider = StateProvider<String>((ref) => 'weight');

final bodyMetricsProvider =
    FutureProvider<List<BodyMetricEntryModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) return const [];
  final repo = ref.watch(progressRepositoryProvider);
  final metricType = ref.watch(selectedMetricTypeProvider);
  return repo.getBodyMetrics(BodyMetricsQuery(
    userId: userId,
    gymId: gymId,
    metricType: metricType,
  ));
});

final progressPhotosProvider =
    FutureProvider<List<ProgressPhotoSetModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) return const [];
  final repo = ref.watch(progressRepositoryProvider);
  return repo.getProgressPhotos(userId: userId, gymId: gymId);
});
