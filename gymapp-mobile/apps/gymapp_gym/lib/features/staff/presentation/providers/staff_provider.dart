import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// BE /gyms/{id}/staff returns {id, gymId, userId, role, firstName, lastName,
/// email, phoneNumber, profilePhotoUrl}. StaffModel speaks {staffRole, name,
/// phone, photoUrl}, so remap field names. Synthesizes a placeholder name only
/// when profile lookup yielded nothing.
Map<String, dynamic> _normalizeStaffJson(Map<String, dynamic> json) {
  final normalized = Map<String, dynamic>.from(json);
  if (normalized['staffRole'] == null && normalized['role'] != null) {
    final role = (normalized['role'] as String).toLowerCase();
    normalized['staffRole'] = role;
  }
  if (normalized['name'] == null) {
    final first = (normalized['firstName'] as String?)?.trim() ?? '';
    final last = (normalized['lastName'] as String?)?.trim() ?? '';
    final fullName = '$first $last'.trim();
    if (fullName.isNotEmpty) {
      normalized['name'] = fullName;
    } else {
      final uid = (normalized['userId'] as String?) ?? '';
      final shortId = uid.length >= 4 ? uid.substring(0, 4) : uid;
      normalized['name'] = shortId.isEmpty ? 'Staff' : 'Staff #$shortId';
    }
  }
  if (normalized['phone'] == null && normalized['phoneNumber'] != null) {
    normalized['phone'] = normalized['phoneNumber'];
  }
  if (normalized['photoUrl'] == null && normalized['profilePhotoUrl'] != null) {
    normalized['photoUrl'] = normalized['profilePhotoUrl'];
  }
  return normalized;
}

final staffListProvider =
    FutureProvider.autoDispose<List<StaffModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);

  return apiClient.get<List<StaffModel>>(
    ApiEndpoints.gymStaff(gymId),
    parser: (data) => (data as List)
        .map((e) => StaffModel.fromJson(_normalizeStaffJson(e as Map<String, dynamic>)))
        .toList(),
  );
});
