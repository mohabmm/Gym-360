import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

// BE CoachLeaderboardEntryDto fields: trainerId, normalizedScore, totalRatings,
// uniqueClients, sessionCount, isCoachOfTheMonth. Flutter model expects additional
// fields (rank, trainerName, averageRating, sessionsThisMonth). Normalize here.
Map<String, dynamic> _normalizeTrainerLeaderboardEntry(
    Map<String, dynamic> json, int index) {
  return {
    ...json,
    'rank': index + 1,
    'trainerName': (json['trainerName'] as String?) ?? 'Trainer',
    'trainerPhotoUrl': json['trainerPhotoUrl'] as String?,
    'averageRating': (json['averageRating'] as num?)?.toDouble() ?? 0.0,
    'sessionsThisMonth': (json['sessionsThisMonth'] as num?)?.toInt() ??
        (json['sessionCount'] as num?)?.toInt() ?? 0,
    'isCoachOfMonth': json['isCoachOfMonth'] as bool? ??
        json['isCoachOfTheMonth'] as bool? ?? false,
  };
}

final trainerLeaderboardProvider =
    FutureProvider.autoDispose<List<TrainerLeaderboardEntryModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return const [];
  return apiClient.get<List<TrainerLeaderboardEntryModel>>(
    ApiEndpoints.trainerLeaderboard,
    queryParameters: {'gymId': gymId},
    parser: (data) {
      final list = data as List;
      return list.asMap().entries
          .map((e) => TrainerLeaderboardEntryModel.fromJson(
              _normalizeTrainerLeaderboardEntry(
                  e.value as Map<String, dynamic>, e.key)))
          .toList();
    },
  );
});

final selectedMemberLeaderboardTypeProvider =
    StateProvider<String>((ref) => 'attendance');

final memberLeaderboardProvider =
    FutureProvider.autoDispose<List<LeaderboardEntryModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return const [];
  final type = ref.watch(selectedMemberLeaderboardTypeProvider);
  final leaderboard = await apiClient.get<LeaderboardModel>(
    ApiEndpoints.leaderboardByType(type),
    queryParameters: {'gymId': gymId},
    parser: (data) => LeaderboardModel.fromJson(data as Map<String, dynamic>),
  );
  return leaderboard.entries;
});

final trainerRatingsDetailProvider =
    FutureProvider.autoDispose.family<List<TrainerRatingModel>, String>(
  (ref, trainerId) async {
    final apiClient = ref.watch(apiClientProvider);
    return apiClient.get<List<TrainerRatingModel>>(
      ApiEndpoints.trainerDetailedRatings(trainerId),
      parser: (data) => (data as List)
          .map((e) => TrainerRatingModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  },
);

final testimonialsProvider =
    FutureProvider.autoDispose<List<PendingTestimonialModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return const [];
  return apiClient.get<List<PendingTestimonialModel>>(
    ApiEndpoints.testimonialsPending,
    queryParameters: {'gymId': gymId},
    parser: (data) => (data as List)
        .map((e) => PendingTestimonialModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});
