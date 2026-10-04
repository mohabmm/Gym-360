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
  // BE now filters server-side by `search` (name/email/phone/memberNumber) and
  // `status` across the WHOLE roster, so members past the first page are
  // findable — no more client-side slice filtering.
  final search = filter.search?.trim() ?? '';
  if (search.isNotEmpty) {
    queryParams['search'] = search;
  }
  if (filter.status != null && filter.status!.isNotEmpty) {
    queryParams['status'] = filter.status;
  }
  if (filter.plan != null && filter.plan!.isNotEmpty) {
    queryParams['plan'] = filter.plan;
  }

  return apiClient.getPaginated<UserModel>(
    ApiEndpoints.gymMembers(gymId),
    params: PaginationParams(
      page: filter.page,
      pageSize: AppConstants.paginationPageSize,
    ),
    additionalParams: queryParams,
    itemParser: (json) => UserModel.fromJson(_normalizeMemberJson(json)),
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
// Per-member payments via the dedicated BE endpoint. Previously this fetched
// only the gym's first 200 payments and filtered client-side, so a member's
// payments were dropped once the gym had more than 200 in total.
final memberPaymentsProvider =
    FutureProvider.autoDispose.family<List<PaymentModel>, String>(
  (ref, memberId) async {
    final apiClient = ref.watch(apiClientProvider);
    return apiClient.get<List<PaymentModel>>(
      ApiEndpoints.billingMemberPayments(memberId),
      parser: (data) => (data as List)
          .map((e) => PaymentModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
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
