import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// BE /gyms/{id}/members returns a member-shaped DTO (memberId, phoneNumber)
/// while the rest of the app speaks UserModel (id, phone). Normalize the keys
/// so a single UserModel.fromJson can consume the response.
Map<String, dynamic> _normalizeMemberJson(Map<String, dynamic> json) {
  final normalized = Map<String, dynamic>.from(json);
  if (normalized['id'] == null && normalized['memberId'] != null) {
    normalized['id'] = normalized['memberId'];
  }
  if (normalized['phone'] == null && normalized['phoneNumber'] != null) {
    normalized['phone'] = normalized['phoneNumber'];
  }
  if (normalized['isFrozen'] == true) {
    normalized['status'] = 'frozen';
  }
  return normalized;
}

// ---------------------------------------------------------------------------
// Member List Filter State
// ---------------------------------------------------------------------------
class MemberFilter {
  final String? search;
  final String? status;
  final String? plan;
  final int page;

  const MemberFilter({
    this.search,
    this.status,
    this.plan,
    this.page = 1,
  });

  MemberFilter copyWith({
    String? search,
    String? status,
    String? plan,
    int? page,
  }) {
    return MemberFilter(
      search: search ?? this.search,
      status: status ?? this.status,
      plan: plan ?? this.plan,
      page: page ?? this.page,
    );
  }
}

final memberFilterProvider = StateProvider<MemberFilter>((ref) {
  return const MemberFilter();
});

// ---------------------------------------------------------------------------
// Paginated Members List
// ---------------------------------------------------------------------------
final membersProvider =
    FutureProvider.autoDispose<PaginatedResult<UserModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  final filter = ref.watch(memberFilterProvider);

  final queryParams = <String, dynamic>{};
  // BE's GET /gyms/{gymId}/members only supports `status` and `section`
  // query filters — `search` is silently dropped. We apply the search
  // term client-side below after the page fetch.
  if (filter.status != null && filter.status!.isNotEmpty) {
    queryParams['status'] = filter.status;
  }
  if (filter.plan != null && filter.plan!.isNotEmpty) {
    queryParams['plan'] = filter.plan;
  }

  final result = await apiClient.getPaginated<UserModel>(
    ApiEndpoints.gymMembers(gymId),
    params: PaginationParams(
      page: filter.page,
      pageSize: AppConstants.paginationPageSize,
    ),
    additionalParams: queryParams,
    itemParser: (json) => UserModel.fromJson(_normalizeMemberJson(json)),
  );

  final search = filter.search?.trim().toLowerCase() ?? '';
  if (search.isEmpty) return result;

  bool matches(UserModel m) {
    final full = '${m.firstName} ${m.lastName}'.toLowerCase();
    if (full.contains(search)) return true;
    if ((m.email ?? '').toLowerCase().contains(search)) return true;
    if ((m.phone ?? '').toLowerCase().contains(search)) return true;
    if ((m.memberNumber ?? '').toLowerCase().contains(search)) return true;
    return false;
  }

  final filtered = result.items.where(matches).toList();
  return PaginatedResult(
    items: filtered,
    meta: PaginationMeta(
      totalCount: filtered.length,
      pageSize: result.meta.pageSize,
      currentPage: result.meta.currentPage,
      totalPages: 1,
    ),
  );
});

// ---------------------------------------------------------------------------
// Member Detail
// ---------------------------------------------------------------------------
final memberDetailProvider =
    FutureProvider.autoDispose.family<UserModel, String>(
  (ref, memberId) async {
    final apiClient = ref.watch(apiClientProvider);
    // migrated to currentGymIdProvider
    final gymId = ref.watch(currentGymIdProvider);
    return apiClient.get<UserModel>(
      ApiEndpoints.gymMemberById(gymId, memberId),
      parser: (data) =>
          UserModel.fromJson(_normalizeMemberJson(data as Map<String, dynamic>)),
    );
  },
);

// ---------------------------------------------------------------------------
// Member Payments
// ---------------------------------------------------------------------------
// BE has no per-member payments endpoint. We fetch the gym's payments via
// `GET /billing/payments?gymId=...` and filter by memberId client-side.
final memberPaymentsProvider =
    FutureProvider.autoDispose.family<List<PaymentModel>, String>(
  (ref, memberId) async {
    final apiClient = ref.watch(apiClientProvider);
    final gymId = ref.watch(currentGymIdProvider);
    final result = await apiClient.getPaginated<PaymentModel>(
      '${ApiEndpoints.basePrefix}/billing/payments',
      params: const PaginationParams(page: 1, pageSize: 200),
      additionalParams: {'gymId': gymId},
      itemParser: (json) => PaymentModel.fromJson(json),
    );
    return result.items.where((p) => p.memberId == memberId).toList();
  },
);

// ---------------------------------------------------------------------------
// Next Member Number suggestion
// ---------------------------------------------------------------------------
final nextMemberNumberProvider = FutureProvider.autoDispose<String>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  // migrated to currentGymIdProvider
  final gymId = ref.watch(currentGymIdProvider);
  try {
    final result = await apiClient.get<String>(
      '${ApiEndpoints.gymMembers(gymId)}/next-number',
      parser: (data) => data.toString(),
    );
    return result;
  } catch (_) {
    return '1';
  }
});
