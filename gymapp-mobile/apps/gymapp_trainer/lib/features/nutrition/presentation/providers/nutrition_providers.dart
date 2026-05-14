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

/// Meal plans created by this trainer.
/// BE requires trainerId + gymId query params.
final trainerMealPlansProvider =
    FutureProvider.autoDispose<List<MealPlanModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  final gymId = ref.watch(activeGymIdProvider);
  if (trainerId == null || gymId == null) return const [];

  return apiClient.get<List<MealPlanModel>>(
    ApiEndpoints.mealPlans,
    queryParameters: {
      'trainerId': trainerId,
      'gymId': gymId,
    },
    parser: (data) => _asList(data)
        .map((e) => MealPlanModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});

/// Food items (Egyptian food database) — BE exposes /nutrition/food-database.
/// Requires page/pageSize.
final foodItemsProvider =
    FutureProvider.autoDispose<List<FoodItemModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<FoodItemModel>>(
    ApiEndpoints.foodDatabase,
    queryParameters: {'page': 1, 'pageSize': 100},
    parser: (data) => _asList(data)
        .map((e) =>
            FoodItemModel.fromFoodDatabaseJson(e as Map<String, dynamic>))
        .toList(),
  );
});

/// Client food log by client ID for a given date (maps to BE
/// GET /api/v1/nutrition/clients/{clientId}/food-log?trainerId=...&gymId=...&date=...).
/// BE returns an empty FoodLogResponse (200) when no log exists — not 404.
final clientNutritionProvider =
    FutureProvider.autoDispose.family<Map<String, dynamic>, String>(
        (ref, clientId) async {
  final apiClient = ref.watch(apiClientProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  final gymId = ref.watch(activeGymIdProvider);
  if (trainerId == null || gymId == null) return const {};

  final now = DateTime.now();
  final date =
      '${now.year}-${now.month.toString().padLeft(2, '0')}-${now.day.toString().padLeft(2, '0')}';

  return apiClient.get<Map<String, dynamic>>(
    ApiEndpoints.clientFoodLog(clientId),
    queryParameters: {
      'trainerId': trainerId,
      'gymId': gymId,
      'date': date,
    },
    parser: (data) {
      if (data is Map<String, dynamic>) return data;
      if (data is List) return {'items': data};
      return const {};
    },
  );
});
