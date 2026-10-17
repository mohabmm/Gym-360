import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/nutrition_remote_datasource.dart';

class NutritionRepository {
  final NutritionRemoteDataSource _remoteDataSource;

  NutritionRepository({required NutritionRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<FoodLogModel> getFoodLog(DailyLogQuery query) =>
      _remoteDataSource.getFoodLog(query);

  /// Returns the food log ID (Guid string).
  Future<String> addFoodEntry(Map<String, dynamic> body) =>
      _remoteDataSource.addFoodEntry(body);

  Future<void> deleteFoodEntry(String entryId) =>
      _remoteDataSource.deleteFoodEntry(entryId);

  Future<WaterLogModel> getWaterLog(DailyLogQuery query) =>
      _remoteDataSource.getWaterLog(query);

  /// Returns the water log entry ID (Guid string).
  Future<String> addWaterEntry(WaterEntryRequest request) =>
      _remoteDataSource.addWaterEntry(request);

  Future<MealPlanModel?> getActiveMealPlan() =>
      _remoteDataSource.getActiveMealPlan();

  Future<List<FoodItemModel>> searchFoodItems(String query) =>
      _remoteDataSource.searchFoodItems(query);
}
