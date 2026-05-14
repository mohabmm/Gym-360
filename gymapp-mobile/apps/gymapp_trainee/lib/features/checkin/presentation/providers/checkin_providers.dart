import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/checkin_remote_datasource.dart';
import '../../data/repositories/checkin_repository.dart';

final checkInRemoteDataSourceProvider =
    Provider<CheckInRemoteDataSource>((ref) {
  return CheckInRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final checkInRepositoryProvider = Provider<CheckInRepository>((ref) {
  return CheckInRepository(
    remoteDataSource: ref.watch(checkInRemoteDataSourceProvider),
  );
});

final checkInHistoryProvider =
    FutureProvider<List<CheckInModel>>((ref) async {
  final authState = ref.watch(authProvider);
  final gymId = authState.valueOrNull?.currentGymId;
  if (gymId == null) return [];

  final repo = ref.watch(checkInRepositoryProvider);
  return repo.getCheckInHistory(gymId);
});
