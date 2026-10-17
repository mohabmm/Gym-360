import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/nutrition_remote_datasource.dart';
import '../../data/repositories/nutrition_repository.dart';

final nutritionRemoteDataSourceProvider =
    Provider<NutritionRemoteDataSource>((ref) {
  return NutritionRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final nutritionRepositoryProvider = Provider<NutritionRepository>((ref) {
  return NutritionRepository(
    remoteDataSource: ref.watch(nutritionRemoteDataSourceProvider),
  );
});

final selectedNutritionDateProvider =
    StateProvider<DateTime>((ref) => DateTime.now());

final foodLogProvider = FutureProvider<FoodLogModel>((ref) async {
  final date = ref.watch(selectedNutritionDateProvider);
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) {
    throw Exception('No authenticated user or active gym');
  }
  final repo = ref.watch(nutritionRepositoryProvider);
  return repo.getFoodLog(
    DailyLogQuery(userId: userId, gymId: gymId, date: date),
  );
});

final waterLogProvider = FutureProvider<WaterLogModel>((ref) async {
  final date = ref.watch(selectedNutritionDateProvider);
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) {
    throw Exception('No authenticated user or active gym');
  }
  final repo = ref.watch(nutritionRepositoryProvider);
  return repo.getWaterLog(
    DailyLogQuery(userId: userId, gymId: gymId, date: date),
  );
});

final activeMealPlanProvider =
    FutureProvider<MealPlanModel?>((ref) async {
  final repo = ref.watch(nutritionRepositoryProvider);
  return repo.getActiveMealPlan();
});

final foodSearchQueryProvider = StateProvider<String>((ref) => '');

final foodSearchResultsProvider =
    FutureProvider<List<FoodItemModel>>((ref) async {
  final query = ref.watch(foodSearchQueryProvider);
  if (query.isEmpty) return [];
  final repo = ref.watch(nutritionRepositoryProvider);
  return repo.searchFoodItems(query);
});
