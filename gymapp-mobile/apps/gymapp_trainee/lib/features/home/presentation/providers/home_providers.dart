import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/home_remote_datasource.dart';
import '../../data/repositories/home_repository.dart';

final homeRemoteDataSourceProvider = Provider<HomeRemoteDataSource>((ref) {
  return HomeRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final homeRepositoryProvider = Provider<HomeRepository>((ref) {
  return HomeRepository(
    remoteDataSource: ref.watch(homeRemoteDataSourceProvider),
  );
});

/// Today's workout -- only non-null if remote mode is active.
final todaysWorkoutProvider = FutureProvider<WorkoutModel?>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) return null;
  final repo = ref.watch(homeRepositoryProvider);
  return repo.getTodaysWorkout(userId: userId, gymId: gymId);
});

/// Upcoming sessions (next 5).
final upcomingSessionsProvider =
    FutureProvider<List<SessionModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) return const [];
  final repo = ref.watch(homeRepositoryProvider);
  return repo.getUpcomingSessions(userId: userId, gymId: gymId);
});

/// Progress dashboard summary for home stats (includes streaks).
final progressDashboardProvider =
    FutureProvider<ProgressDashboardModel>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) {
    throw Exception('No authenticated user or active gym');
  }
  final repo = ref.watch(homeRepositoryProvider);
  return repo.getProgressDashboard(userId: userId, gymId: gymId);
});

/// Today's water intake.
final todaysWaterProvider = FutureProvider<WaterLogModel>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) {
    throw Exception('No authenticated user or active gym');
  }
  final repo = ref.watch(homeRepositoryProvider);
  return repo.getTodaysWater(userId: userId, gymId: gymId);
});
