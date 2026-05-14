import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/membership_remote_datasource.dart';
import '../../data/repositories/membership_repository.dart';

final membershipRemoteDataSourceProvider =
    Provider<MembershipRemoteDataSource>((ref) {
  return MembershipRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final membershipRepositoryProvider = Provider<MembershipRepository>((ref) {
  return MembershipRepository(
    remoteDataSource: ref.watch(membershipRemoteDataSourceProvider),
  );
});

final activePackagesProvider =
    FutureProvider<List<SessionPackageModel>>((ref) async {
  final repo = ref.watch(membershipRepositoryProvider);
  return repo.getActivePackages();
});

final paymentHistoryProvider =
    FutureProvider<List<PaymentModel>>((ref) async {
  final repo = ref.watch(membershipRepositoryProvider);
  return repo.getPayments();
});
