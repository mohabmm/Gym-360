import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// Shape returned by `GET /gyms/{gymId}/kiosk-pin`.
class KioskPinInfo {
  final bool isSet;
  final String? hash;

  const KioskPinInfo({required this.isSet, this.hash});

  factory KioskPinInfo.fromJson(Map<String, dynamic> json) => KioskPinInfo(
        isSet: json['isSet'] as bool? ?? false,
        hash: json['hash'] as String?,
      );
}

/// Pulls kiosk PIN state (including hash for offline verify) and caches the
/// hash in secure storage keyed by gymId. Callers should `ref.invalidate(...)`
/// this provider after mutations.
final kioskPinInfoProvider =
    FutureProvider.autoDispose<KioskPinInfo>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  final storage = ref.watch(secureStorageProvider);

  final info = await apiClient.get<KioskPinInfo>(
    ApiEndpoints.kioskPin(gymId),
    parser: (data) => KioskPinInfo.fromJson(data as Map<String, dynamic>),
  );

  // Cache hash for offline exit. Delete the key when no PIN is set so a stale
  // hash never survives a "remove PIN" action on another device.
  final cacheKey = StorageKeys.kioskPinHashFor(gymId);
  if (info.isSet && info.hash != null) {
    await storage.write(cacheKey, info.hash!);
  } else {
    await storage.deleteKey(cacheKey);
  }

  return info;
});

final gymSettingsProvider =
    FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<Map<String, dynamic>>(
    ApiEndpoints.gymById(gymId),
    parser: (data) => data as Map<String, dynamic>,
  );
});

// GET /gyms/{id}/sections → [{ id, sectionType: Mens|Womens|Mixed, name, isActive }].
// Resolves to the lower-cased types that are currently active.
final gymSectionsProvider =
    FutureProvider.autoDispose<Set<String>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return const <String>{};
  return apiClient.get<Set<String>>(
    ApiEndpoints.gymSections(gymId),
    parser: (data) => (data as List)
        .whereType<Map>()
        .where((s) => s['isActive'] != false)
        .map((s) => (s['sectionType'] ?? '').toString().toLowerCase())
        .where((t) => t.isNotEmpty)
        .toSet(),
  );
});

// No dedicated seasonal-hours sub-resource endpoint exists on the backend.
final gymSeasonalHoursProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((_) async =>
        const <Map<String, dynamic>>[]);

final gymSessionPoliciesProvider =
    FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<Map<String, dynamic>>(
    ApiEndpoints.gymSessionPolicy(gymId),
    parser: (data) => data as Map<String, dynamic>,
  );
});

// Amenities live inside the main gym document. Fetch GET /gyms/{id} and extract.
final gymAmenitiesProvider =
    FutureProvider.autoDispose<List<String>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<List<String>>(
    ApiEndpoints.gymById(gymId),
    parser: (data) {
      final map = data as Map<String, dynamic>;
      final amenities = map['amenities'];
      if (amenities == null) return <String>[];
      return (amenities as List).map((e) => e.toString()).toList();
    },
  );
});

// No dedicated operating-hours sub-resource endpoint exists on the backend.
final gymOperatingHoursProvider =
    FutureProvider.autoDispose<Map<String, dynamic>>((_) async =>
        const <String, dynamic>{});

/// Reads the gym-level testing-mode flag (early_session_start override).
/// Owner/manager can flip it via [setTestingModeProvider]; receptionists are 403.
final testingModeProvider =
    FutureProvider.autoDispose<bool>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<bool>(
    ApiEndpoints.gymTestingMode(gymId),
    parser: (data) {
      if (data is Map<String, dynamic>) {
        return data['enabled'] == true;
      }
      return false;
    },
  );
});

/// Writes the gym-level testing-mode flag. The trainer app's local toggle
/// reflects this value (read-only on that side) — only the staff app (owner
/// or manager) is allowed to change it.
final setTestingModeProvider = Provider((ref) {
  final apiClient = ref.watch(apiClientProvider);
  return (String gymId, bool enabled) async {
    await apiClient.postVoid(
      ApiEndpoints.gymTestingMode(gymId),
      data: {'enabled': enabled},
    );
    ref.invalidate(testingModeProvider);
  };
});
