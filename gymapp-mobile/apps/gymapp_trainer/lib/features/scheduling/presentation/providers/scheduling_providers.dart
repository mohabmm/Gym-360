import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

List<dynamic> _asList(dynamic data) {
  if (data is List) return data;
  if (data is Map<String, dynamic> && data['items'] is List) {
    return data['items'] as List<dynamic>;
  }
  return const [];
}

/// Trainer availability (weekly slots + blocked dates).
/// BE returns an AvailabilityResponse object with a `weeklySlots` array.
final availabilitySlotsProvider =
    FutureProvider.autoDispose<List<TrainerAvailabilityModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  if (gymId == null || trainerId == null) return <TrainerAvailabilityModel>[];

  try {
    return await apiClient.get<List<TrainerAvailabilityModel>>(
      ApiEndpoints.trainerAvailability,
      queryParameters: {'trainerId': trainerId, 'gymId': gymId},
      parser: (data) {
        // BE returns either a list of slots or an object containing weeklySlots.
        final List<dynamic> list;
        if (data is List) {
          list = data;
        } else if (data is Map<String, dynamic>) {
          final slots = data['weeklySlots'] ?? data['slots'] ?? data['items'];
          list = slots is List ? slots : const [];
        } else {
          list = const [];
        }
        return list
            .map((e) =>
                TrainerAvailabilityModel.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  } on ApiException catch (e) {
    // Trainer with no availability config yet: treat as empty grid.
    if (e.statusCode == 404) return <TrainerAvailabilityModel>[];
    rethrow;
  }
});

/// Pending booking requests for this trainer — BE returns them via the
/// sessions endpoint filtered by status=pending.
final pendingBookingRequestsProvider =
    FutureProvider.autoDispose<List<SessionModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  if (gymId == null || trainerId == null) return <SessionModel>[];

  return apiClient.get<List<SessionModel>>(
    ApiEndpoints.sessions,
    queryParameters: {
      'gymId': gymId,
      'trainerId': trainerId,
      'status': 'pending',
      'page': 1,
      'pageSize': 50,
    },
    parser: (data) => _asList(data)
        .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});

/// All upcoming sessions for the trainer.
final upcomingSessionsProvider =
    FutureProvider.autoDispose<List<SessionModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  if (gymId == null || trainerId == null) return <SessionModel>[];

  return apiClient.get<List<SessionModel>>(
    ApiEndpoints.upcomingSessions,
    queryParameters: {
      'userId': trainerId,
      'gymId': gymId,
      'role': 'trainer',
    },
    parser: (data) => _asList(data)
        .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});
