import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// My ratings summary — BE exposes this via the trainer public profile.
final myRatingsSummaryProvider =
    FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  if (gymId == null || trainerId == null) return const {};

  return apiClient.get<Map<String, dynamic>>(
    ApiEndpoints.trainerPublicProfile(trainerId),
    queryParameters: {'gymId': gymId},
    parser: (data) => data is Map<String, dynamic> ? data : {},
  );
});

/// Trainer testimonials from public profile.
/// Per docs visibility matrix, trainers do NOT see individual ratings
/// (those are gym-manager only). Trainers see their public profile
/// which includes aggregated rating + anonymized testimonials.
final myTestimonialsProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final auth = ref.watch(authProvider);
  final gymId = ref.watch(activeGymIdProvider);
  if (auth.user == null || gymId == null) return [];

  final profile = await apiClient.get<Map<String, dynamic>>(
    ApiEndpoints.trainerPublicProfile(auth.user!.id),
    queryParameters: {'gymId': gymId},
    parser: (data) => data is Map<String, dynamic> ? data : {},
  );

  final testimonials = profile['testimonials'] ?? profile['anonymizedTestimonials'];
  if (testimonials is List) {
    return testimonials.cast<Map<String, dynamic>>();
  }
  return [];
});
