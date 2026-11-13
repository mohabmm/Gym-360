import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/progress_journey_api.dart';
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

final progressJourneyApiProvider = Provider<ProgressJourneyApi>((ref) {
  return ProgressJourneyApi(apiClient: ref.watch(apiClientProvider));
});

/// pt.progress_measurements: progress since day one (all entries + goal).
final progressJourneyProvider = FutureProvider<ProgressJourney>((ref) async {
  final gymId = ref.watch(authProvider).valueOrNull?.currentGymId;
  if (gymId == null || gymId.isEmpty) {
    return ProgressJourney.from(const ProgressJourneyModel(userId: '', entries: []));
  }
  final model = await ref.watch(progressJourneyApiProvider).getJourney(GymId(gymId));
  return ProgressJourney.from(model);
});

/// The member's assigned coach at the current gym, to name coach entries.
final assignedCoachProvider = Provider<({String? id, String? name})>((ref) {
  final auth = ref.watch(authProvider).valueOrNull;
  final membership = (auth?.user?.gymMemberships ?? const <GymMembershipModel>[])
      .where((m) => m.gymId == auth?.currentGymId)
      .firstOrNull;
  return (id: membership?.assignedTrainerId, name: membership?.assignedTrainerName);
});

/// Refreshes everything that shows body metrics after a new entry or goal.
void refreshProgress(WidgetRef ref) {
  ref.invalidate(progressJourneyProvider);
  ref.invalidate(progressDashboardDetailProvider);
  ref.invalidate(bodyMetricsProvider);
}

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
