import 'package:gymapp_core/gymapp_core.dart';

import '../models/nutrition_requests.dart';

export '../models/nutrition_requests.dart';

class NutritionRemoteDataSource {
  final ApiClient _apiClient;

  NutritionRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  Future<FoodLogModel> getFoodLog(DailyLogQuery query) async {
    return _apiClient.get<FoodLogModel>(
      ApiEndpoints.foodLog,
      queryParameters: query.toQueryParameters(),
      parser: (data) => _foodLogFromDto(data as Map<String, dynamic>),
    );
  }

  /// BE POST /nutrition/food-log returns Guid (food log ID), 201.
  /// Expects: userId, gymId, date, mealType, foodItems.
  Future<String> addFoodEntry(Map<String, dynamic> body) async {
    return _apiClient.post<String>(
      ApiEndpoints.foodLog,
      data: body,
      parser: (data) => data.toString(),
    );
  }

  /// Deletes a single logged food item.
  ///
  /// [entryId] is the synthetic id the food-log parser assigns each item,
  /// `"<mealId>:<index>"` (see `_foodLogFromDto`). Food items have no stable
  /// server id (JSON value objects), so the BE addresses them by meal + index:
  /// `DELETE /nutrition/food-log/meals/{mealId}/items/{index}`.
  Future<void> deleteFoodEntry(String entryId) async {
    final sep = entryId.lastIndexOf(':');
    if (sep <= 0 || sep == entryId.length - 1) {
      throw ArgumentError('Invalid food entry id: "$entryId"');
    }
    final mealId = entryId.substring(0, sep);
    final index = entryId.substring(sep + 1);
    await _apiClient.delete('${ApiEndpoints.foodLog}/meals/$mealId/items/$index');
  }

  Future<WaterLogModel> getWaterLog(DailyLogQuery query) async {
    return _apiClient.get<WaterLogModel>(
      ApiEndpoints.waterLog,
      queryParameters: query.toQueryParameters(),
      parser: (data) => WaterLogModel.fromJson(data as Map<String, dynamic>),
    );
  }

  /// BE POST /nutrition/water-log returns Guid (water log entry ID), 201.
  Future<String> addWaterEntry(WaterEntryRequest request) async {
    return _apiClient.post<String>(
      ApiEndpoints.waterLog,
      data: request.toJson(),
      parser: (data) => data.toString(),
    );
  }

  /// Returns the trainee's currently-assigned meal plan at the current gym,
  /// or `null` if none is assigned.
  ///
  /// Backed by `GET /api/v1/nutrition/me/meal-plan`. `X-Gym-Id` is supplied
  /// automatically by [GymIdInterceptor].
  Future<MealPlanModel?> getActiveMealPlan() async {
    return _apiClient.get<MealPlanModel?>(
      ApiEndpoints.myMealPlan,
      parser: _mealPlanFromRaw,
    );
  }

  Future<List<FoodItemModel>> searchFoodItems(
    String query, {
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<FoodItemModel>>(
      ApiEndpoints.foodDatabase,
      queryParameters: {
        'query': query,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => _itemsOf(data)
          .map<FoodItemModel>((e) =>
              FoodItemModel.fromFoodDatabaseJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// A bare list, or the `items` of a paged `{ items: [...] }` payload.
List<dynamic> _itemsOf(dynamic data) {
  if (data is List) return data;
  if (data is Map && data['items'] is List) return data['items'] as List;
  return const [];
}

/// `null` when nothing is assigned (empty body or a bare envelope).
MealPlanModel? _mealPlanFromRaw(dynamic raw) {
  if (raw is! Map<String, dynamic>) return null;
  final looksLikeEnvelope =
      !raw.containsKey('id') && (raw.containsKey('errors') || raw.containsKey('meta'));
  if (looksLikeEnvelope) return null;
  // BE sometimes emits macro/calorie fields as decimals (150.00) while
  // the model declares them as int. Coerce numerics via a normaliser
  // so freezed's generated fromJson doesn't throw a TypeError.
  return MealPlanModel.fromJson(_normaliseMealPlanJson(raw));
}

/// Coerce any numeric meal-plan field that the model types as `int` but the
/// backend may emit as a decimal (e.g. `150.00`). We rewrite only the fields
/// where this mismatch has been observed; untouched fields round-trip as-is.
Map<String, dynamic> _normaliseMealPlanJson(Map<String, dynamic> raw) {
  final out = Map<String, dynamic>.from(raw);
  final dailyCal = out['dailyCalorieTarget'];
  if (dailyCal is num) out['dailyCalorieTarget'] = dailyCal.toInt();
  final macros = out['macroTargets'];
  if (macros is Map<String, dynamic>) {
    final m = Map<String, dynamic>.from(macros);
    for (final key in const ['proteinGrams', 'carbsGrams', 'fatGrams']) {
      final v = m[key];
      if (v is num) m[key] = v.toInt();
    }
    out['macroTargets'] = m;
  }
  return out;
}

/// Adapts backend `FoodLogResponse` — grouped `meals`, each with its own
/// `foodItems` — into the `FoodLogModel` the client uses, which is flat
/// `entries`. Food items on the BE lack a stable `id`, so we synthesise one
/// from `"<mealId>:<index>"` to satisfy the model's required `id`.
FoodLogModel _foodLogFromDto(Map<String, dynamic> dto) {
  final meals = dto['meals'];
  final entries = <FoodLogEntryModel>[
    if (meals is List)
      for (final rawMeal in meals)
        ..._mealEntries(rawMeal as Map<String, dynamic>),
  ];

  return FoodLogModel(
    id: (dto['id'] ?? '').toString(),
    userId: (dto['userId'] ?? '').toString(),
    date: DateTime.parse((dto['date'] ?? DateTime.now().toIso8601String()).toString()),
    entries: entries,
    totalCalories: (dto['totalCalories'] as num?)?.toInt(),
    totalProtein: (dto['totalProtein'] as num?)?.toDouble(),
    totalCarbs: (dto['totalCarbs'] as num?)?.toDouble(),
    totalFat: (dto['totalFat'] as num?)?.toDouble(),
  );
}

/// Flattens one meal's `foodItems` into entries with `"<mealId>:<index>"` ids.
List<FoodLogEntryModel> _mealEntries(Map<String, dynamic> meal) {
  final mealId = (meal['id'] ?? '').toString();
  final mealType = (meal['mealType'] ?? '').toString();
  final foodItems = meal['foodItems'];
  if (foodItems is! List) return const [];
  return [
    for (var i = 0; i < foodItems.length; i++)
      _entryFromItem(
        foodItems[i] as Map<String, dynamic>,
        id: '$mealId:$i',
        mealType: mealType,
      ),
  ];
}

FoodLogEntryModel _entryFromItem(
  Map<String, dynamic> item, {
  required String id,
  required String mealType,
}) {
  return FoodLogEntryModel(
    id: id,
    mealType: mealType,
    name: (item['name'] ?? '').toString(),
    quantity: (item['quantity'] as num?)?.toDouble() ?? 0,
    unit: (item['unit'] ?? '').toString(),
    calories: (item['calories'] as num?)?.toInt() ?? 0,
    protein: (item['protein'] as num?)?.toDouble(),
    carbs: (item['carbs'] as num?)?.toDouble(),
    fat: (item['fat'] as num?)?.toDouble(),
  );
}
