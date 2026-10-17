import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

String _requireGymId(Ref ref) {
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) {
    throw Exception('No active gym selected');
  }
  return gymId;
}

const _kDayOfWeekIndex = <String, int>{
  'monday': DateTime.monday,
  'tuesday': DateTime.tuesday,
  'wednesday': DateTime.wednesday,
  'thursday': DateTime.thursday,
  'friday': DateTime.friday,
  'saturday': DateTime.saturday,
  'sunday': DateTime.sunday,
};

DateTime _nextOccurrence(int weekday, int hour, int minute, int second) {
  final now = DateTime.now();
  var diff = (weekday - now.weekday) % 7;
  if (diff < 0) diff += 7;
  final candidate = DateTime(now.year, now.month, now.day, hour, minute, second)
      .add(Duration(days: diff));
  return candidate.isBefore(now)
      ? candidate.add(const Duration(days: 7))
      : candidate;
}

/// BE /gyms/{id}/classes returns a nested-schedule shape; the Flutter
/// GymClassModel expects flat startTime/endTime + instructorName. Map the
/// response so a single fromJson can consume it.
Map<String, dynamic> _normalizeClassJson(Map<String, dynamic> json) {
  final normalized = Map<String, dynamic>.from(json);

  final schedule = json['schedule'];
  if (schedule is Map<String, dynamic>) {
    final dayName = (schedule['dayOfWeek'] as String?)?.toLowerCase() ?? '';
    final weekday = _kDayOfWeekIndex[dayName] ?? DateTime.monday;
    final timeStr = (schedule['startTime'] as String?) ?? '00:00:00';
    final parts = timeStr.split(':');
    final hour = parts.isNotEmpty ? int.tryParse(parts[0]) ?? 0 : 0;
    final minute = parts.length > 1 ? int.tryParse(parts[1]) ?? 0 : 0;
    final second = parts.length > 2 ? int.tryParse(parts[2]) ?? 0 : 0;
    final start = _nextOccurrence(weekday, hour, minute, second);
    final duration = (schedule['durationMinutes'] as num?)?.toInt() ?? 60;
    normalized['startTime'] ??= start.toIso8601String();
    normalized['endTime'] ??=
        start.add(Duration(minutes: duration)).toIso8601String();
    normalized['isRecurring'] ??= schedule['isRecurring'] ?? false;
    normalized['recurringDayOfWeek'] ??= weekday;
  }

  // GymClassModel requires a non-null instructorName. BE now returns
  // `trainerName` (full name) on the class DTO; fall back to the synthesized
  // placeholder only when the trainer profile lookup yielded nothing.
  if (normalized['instructorName'] == null) {
    final beTrainerName = (normalized['trainerName'] as String?)?.trim();
    if (beTrainerName != null && beTrainerName.isNotEmpty) {
      normalized['instructorName'] = beTrainerName;
    } else {
      final tid = (normalized['trainerId'] ?? normalized['instructorId'])
          as String?;
      final shortId =
          tid != null && tid.length >= 4 ? tid.substring(0, 4) : '';
      normalized['instructorName'] =
          shortId.isEmpty ? 'Trainer' : 'Trainer #$shortId';
    }
  }
  if (normalized['instructorId'] == null && normalized['trainerId'] != null) {
    normalized['instructorId'] = normalized['trainerId'];
  }

  return normalized;
}

final classesProvider =
    FutureProvider.autoDispose<List<GymClassModel>>((ref) async {
  final gymId = _requireGymId(ref);
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<GymClassModel>>(
    ApiEndpoints.gymClasses(GymId(gymId)),
    parser: (data) => (data as List)
        .map((e) => GymClassModel.fromJson(_normalizeClassJson(e as Map<String, dynamic>)))
        .toList(),
  );
});

final classDetailProvider =
    FutureProvider.autoDispose.family<GymClassModel, String>(
  (ref, classId) async {
    final gymId = _requireGymId(ref);
    final apiClient = ref.watch(apiClientProvider);
    return apiClient.get<GymClassModel>(
      ApiEndpoints.gymClassById(GymId(gymId), ClassId(classId)),
      parser: (data) => GymClassModel.fromJson(
          _normalizeClassJson(data as Map<String, dynamic>)),
    );
  },
);

/// BE returns a MemberDto shape (memberId, phoneNumber). UserModel expects
/// (id, phone). Mirror the normalization used by members_provider.
Map<String, dynamic> _normalizeMemberJson(Map<String, dynamic> json) {
  final normalized = Map<String, dynamic>.from(json);
  if (normalized['id'] == null && normalized['memberId'] != null) {
    normalized['id'] = normalized['memberId'];
  }
  if (normalized['phone'] == null && normalized['phoneNumber'] != null) {
    normalized['phone'] = normalized['phoneNumber'];
  }
  return normalized;
}

final classAttendanceProvider =
    FutureProvider.autoDispose.family<List<UserModel>, String>(
  (ref, classId) async {
    final gymId = _requireGymId(ref);
    final apiClient = ref.watch(apiClientProvider);
    return apiClient.get<List<UserModel>>(
      ApiEndpoints.gymClassAttendance(GymId(gymId), ClassId(classId)),
      parser: (data) => (data as List)
          .map((e) =>
              UserModel.fromJson(_normalizeMemberJson(e as Map<String, dynamic>)))
          .toList(),
    );
  },
);

final classWaitlistProvider =
    FutureProvider.autoDispose.family<List<UserModel>, String>(
  (ref, classId) async {
    final gymId = _requireGymId(ref);
    final apiClient = ref.watch(apiClientProvider);
    return apiClient.get<List<UserModel>>(
      ApiEndpoints.gymClassWaitlist(GymId(gymId), ClassId(classId)),
      parser: (data) => (data as List)
          .map((e) =>
              UserModel.fromJson(_normalizeMemberJson(e as Map<String, dynamic>)))
          .toList(),
    );
  },
);
