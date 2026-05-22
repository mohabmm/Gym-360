import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/nutrition_remote_datasource.dart';

class NutritionRepository {
  final NutritionRemoteDataSource _remoteDataSource;

  NutritionRepository({required NutritionRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<FoodLogModel> getFoodLog({
    required String userId,
    required String gymId,
    required DateTime date,
  }) =>
      _remoteDataSource.getFoodLog(userId: userId, gymId: gymId, date: date);

  /// Returns the food log ID (Guid string).
  Future<String> addFoodEntry(Map<String, dynamic> body) =>
      _remoteDataSource.addFoodEntry(body);

  Future<void> deleteFoodEntry(String entryId) =>
      _remoteDataSource.deleteFoodEntry(entryId);

  Future<WaterLogModel> getWaterLog({
    required String userId,
    required String gymId,
    required DateTime date,
  }) =>
      _remoteDataSource.getWaterLog(userId: userId, gymId: gymId, date: date);

  /// Returns the water log entry ID (Guid string).
  Future<String> addWaterEntry({
    required String userId,
    required String gymId,
    required DateTime date,
    required int amountMl,
  }) =>
      _remoteDataSource.addWaterEntry(
        userId: userId,
        gymId: gymId,
        date: date,
        amountMl: amountMl,
      );

  Future<MealPlanModel?> getActiveMealPlan() =>
      _remoteDataSource.getActiveMealPlan();

  Future<List<FoodItemModel>> searchFoodItems(String query) =>
      _remoteDataSource.searchFoodItems(query);
}
