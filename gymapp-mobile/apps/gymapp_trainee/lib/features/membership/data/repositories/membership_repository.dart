import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/membership_remote_datasource.dart';

class MembershipRepository {
  final MembershipRemoteDataSource _remoteDataSource;

  MembershipRepository(
      {required MembershipRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<List<SessionPackageModel>> getActivePackages() =>
      _remoteDataSource.getActivePackages();

  /// Paginated payment history. Exposes [PaginatedResult] so callers can
  /// drive infinite-scroll / paging UIs; legacy callers that only need the
  /// first page should use [getPayments].
  Future<PaginatedResult<PaymentModel>> getPaymentsPage({
    int page = 1,
    int pageSize = 20,
  }) =>
      _remoteDataSource.getPayments(page: page, pageSize: pageSize);

  /// Back-compat shim: returns the first page of payments as a plain list.
  Future<List<PaymentModel>> getPayments() async {
    final result = await _remoteDataSource.getPayments();
    return result.items;
  }
}
