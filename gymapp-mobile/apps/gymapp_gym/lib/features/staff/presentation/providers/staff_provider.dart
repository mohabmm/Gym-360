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
    normalized['staffRole'] = (normalized['role'] as String).toLowerCase();
  }
  normalized['name'] ??= _fallbackStaffName(normalized);
  _copyIfMissing(normalized, from: 'phoneNumber', to: 'phone');
  _copyIfMissing(normalized, from: 'profilePhotoUrl', to: 'photoUrl');
  return normalized;
}

/// "First Last", else "Staff #<first 4 of userId>", else "Staff".
String _fallbackStaffName(Map<String, dynamic> json) {
  final first = (json['firstName'] as String?)?.trim() ?? '';
  final last = (json['lastName'] as String?)?.trim() ?? '';
  final fullName = '$first $last'.trim();
  if (fullName.isNotEmpty) return fullName;
  final uid = (json['userId'] as String?) ?? '';
  final shortId = uid.length >= 4 ? uid.substring(0, 4) : uid;
  return shortId.isEmpty ? 'Staff' : 'Staff #$shortId';
}

/// Fills [to] from the legacy key [from] when [to] is absent.
void _copyIfMissing(
  Map<String, dynamic> json, {
  required String from,
  required String to,
}) {
  if (json[to] == null && json[from] != null) json[to] = json[from];
}

final staffListProvider =
    FutureProvider.autoDispose<List<StaffModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);

  return apiClient.get<List<StaffModel>>(
    ApiEndpoints.gymStaff(GymId(gymId)),
    parser: (data) => (data as List)
        .map((e) => StaffModel.fromJson(_normalizeStaffJson(e as Map<String, dynamic>)))
        .toList(),
  );
});
