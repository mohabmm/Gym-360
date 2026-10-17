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

  /// Switches the status filter and returns to page 1. Unlike [copyWith],
  /// passing null clears the filter ("All").
  MemberFilter withStatus(String? status) {
    return MemberFilter(search: search, status: status, plan: plan);
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
    ApiEndpoints.gymMembers(GymId(gymId)),
    params: PaginationParams(
      page: filter.page,
      pageSize: AppConstants.paginationPageSize,
    ),
    additionalParams: queryParams,
    itemParser: (json) => UserModel.fromJson(_normalizeMemberJson(json)),
  );
});

// ---------------------------------------------------------------------------
// Member lookup by number (payment / package desks)
// ---------------------------------------------------------------------------
/// Finds the member whose member number (or id) staff typed, searching the
/// whole roster server-side. Returns null when nobody matches.
///
/// Numbers are compared ignoring leading zeros so "2" finds "0002".
Future<UserModel?> findMemberByNumber(
  ApiClient apiClient,
  String gymId,
  String input,
) async {
  final query = input.trim();
  if (query.isEmpty || gymId.isEmpty) return null;

  final result = await apiClient.getPaginated<UserModel>(
    ApiEndpoints.gymMembers(GymId(gymId)),
    params: const PaginationParams(page: 1, pageSize: 50),
    additionalParams: {'search': query},
    itemParser: (json) => UserModel.fromJson(_normalizeMemberJson(json)),
  );

  String canonical(String value) {
    final trimmed = value.trim().toLowerCase();
    final asInt = int.tryParse(trimmed);
    return asInt?.toString() ?? trimmed;
  }

  final wanted = canonical(query);
  for (final member in result.items) {
    final number = member.memberNumber;
    if (member.id == query ||
        (number != null && canonical(number) == wanted)) {
      return member;
    }
  }
  return null;
}

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
      ApiEndpoints.gymMemberById(GymId(gymId), MemberId(memberId)),
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
      ApiEndpoints.billingMemberPayments(MemberId(memberId)),
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
      '${ApiEndpoints.gymMembers(GymId(gymId))}/next-number',
      parser: (data) => data.toString(),
    );
    return result;
  } catch (_) {
    return '1';
  }
});
