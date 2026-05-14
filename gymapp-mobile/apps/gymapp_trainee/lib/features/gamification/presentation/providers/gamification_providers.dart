import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/gamification_remote_datasource.dart';
import '../../data/repositories/gamification_repository.dart';

final gamificationRemoteDataSourceProvider =
    Provider<GamificationRemoteDataSource>((ref) {
  return GamificationRemoteDataSource(
      apiClient: ref.watch(apiClientProvider));
});

final gamificationRepositoryProvider =
    Provider<GamificationRepository>((ref) {
  return GamificationRepository(
    remoteDataSource: ref.watch(gamificationRemoteDataSourceProvider),
  );
});

final selectedLeaderboardTypeProvider =
    StateProvider<String>((ref) => 'attendance');

final leaderboardProvider =
    FutureProvider<LeaderboardModel>((ref) async {
  final type = ref.watch(selectedLeaderboardTypeProvider);
  final gymId = ref.watch(authProvider).valueOrNull?.currentGymId;
  if (gymId == null) throw Exception('No active gym');
  final repo = ref.watch(gamificationRepositoryProvider);
  return repo.getLeaderboard(type, gymId: gymId);
});

final trainerPublicProfileProvider =
    FutureProvider.family<TrainerPublicProfileModel, String>(
        (ref, trainerId) async {
  final gymId = ref.watch(authProvider).valueOrNull?.currentGymId;
  if (gymId == null) throw Exception('No active gym');
  final repo = ref.watch(gamificationRepositoryProvider);
  return repo.getTrainerPublicProfile(trainerId, gymId: gymId);
});
