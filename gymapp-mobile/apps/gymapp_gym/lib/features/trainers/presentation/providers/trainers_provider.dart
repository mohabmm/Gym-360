import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// BE /gyms/{id}/trainers now enriches the DTO with profile fields, but still
/// uses `trainerId`/`phoneNumber` keys. Map those into UserModel's expected
/// `id`/`phone` shape. Falls back to a placeholder name only if profile lookup
/// returned nothing (deactivated user, race during signup, etc).
UserModel _trainerStubFromJson(Map<String, dynamic> json) {
  final trainerId = (json['trainerId'] ?? json['id'] ?? '') as String;
  final firstName = json['firstName'] as String?;
  final lastName = json['lastName'] as String?;
  final shortId =
      trainerId.length >= 4 ? trainerId.substring(0, 4) : trainerId;
  return UserModel(
    id: trainerId,
    email: (json['email'] as String?) ?? '',
    firstName: firstName?.trim().isNotEmpty == true ? firstName! : 'Trainer',
    lastName:
        lastName?.trim().isNotEmpty == true ? lastName! : '#$shortId',
    phone: (json['phone'] as String?) ?? (json['phoneNumber'] as String?),
    profilePhotoUrl: json['profilePhotoUrl'] as String?,
    role: (json['role'] as String?) ?? 'trainer',
    status: json['status'] as String?,
    gymId: json['gymId'] as String?,
  );
}

final trainersProvider =
    FutureProvider.autoDispose<List<UserModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  // migrated to currentGymIdProvider
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<List<UserModel>>(
    ApiEndpoints.gymTrainers(gymId),
    parser: (data) => (data as List)
        .map((e) => _trainerStubFromJson(e as Map<String, dynamic>))
        .toList(),
  );
});

// BE has no single-trainer endpoint. Derive detail from the gym trainers list.
final trainerDetailProvider =
    FutureProvider.autoDispose.family<UserModel, String>(
  (ref, trainerId) async {
    final trainers = await ref.watch(trainersProvider.future);
    return trainers.firstWhere(
      (t) => t.id == trainerId,
      orElse: () => throw StateError('Trainer $trainerId not found'),
    );
  },
);

final pendingTrainersProvider =
    FutureProvider.autoDispose<List<UserModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  // migrated to currentGymIdProvider
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<List<UserModel>>(
    ApiEndpoints.gymTrainers(gymId),
    queryParameters: const {'status': 'pending'},
    parser: (data) => (data as List)
        .map((e) => _trainerStubFromJson(e as Map<String, dynamic>))
        .where((t) => t.status?.toLowerCase() == 'pending')
        .toList(),
  );
});

final trainerRatingsProvider =
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
