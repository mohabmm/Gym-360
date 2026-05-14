import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/settings_remote_datasource.dart';
import '../../data/repositories/settings_repository.dart';

final settingsRemoteDataSourceProvider =
    Provider<SettingsRemoteDataSource>((ref) {
  return SettingsRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final settingsRepositoryProvider = Provider<SettingsRepository>((ref) {
  return SettingsRepository(
    remoteDataSource: ref.watch(settingsRemoteDataSourceProvider),
  );
});

final consentsProvider =
    FutureProvider<List<UserConsentModel>>((ref) async {
  final repo = ref.watch(settingsRepositoryProvider);
  return repo.getConsents();
});
