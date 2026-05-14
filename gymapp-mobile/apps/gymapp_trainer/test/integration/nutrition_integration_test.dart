import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers MealPlanBuilderScreen and ClientNutritionViewScreen.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('MealPlanBuilderScreen', () {
    test('GET /nutrition/meal-plans returns list', () async {
      t.adapter.onGet(
        ApiEndpoints.mealPlans,
        respond: (_) => t.adapter.ok([Samples.mealPlan()]),
      );

      final plans = await t.apiClient.get<List<MealPlanModel>>(
        ApiEndpoints.mealPlans,
        parser: (data) => (data as List<dynamic>)
            .map((e) => MealPlanModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.path, '/api/v1/nutrition/meal-plans');
      expect(plans.single.name, 'Cut plan');
      expect(plans.single.dailyCalorieTarget, 2000);
    });

    test('POST /nutrition/meal-plans creates plan with macro targets',
        () async {
      t.adapter.onPost(
        ApiEndpoints.mealPlans,
        respond: (_) => t.adapter.created(Samples.mealPlan()),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.mealPlans,
        data: {
          'trainerId': kTrainerId,
          'gymId': kGymId,
          'name': 'Cut plan',
          'description': null,
          'dailyCalorieTarget': 2000,
          'macroTargets': {
            'proteinGrams': 150,
            'carbsGrams': 225,
            'fatGrams': 56,
          },
          'days': <Map<String, dynamic>>[],
          'isTemplate': false,
        },
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['trainerId'], kTrainerId);
      expect(body['dailyCalorieTarget'], 2000);
      expect((body['macroTargets'] as Map)['proteinGrams'], 150);
      expect(body['isTemplate'], false);
    });

    test('POST /nutrition/meal-plans/{id}/assign sends traineeId', () async {
      t.adapter.onPost(
        ApiEndpoints.assignMealPlan('mp-1'),
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.assignMealPlan('mp-1'),
        data: {'traineeId': kClientId},
      );

      expect(t.onlyRequest.path, '/api/v1/nutrition/meal-plans/mp-1/assign');
      expect(t.onlyRequest.bodyAsMap, {'traineeId': kClientId});
    });
  });

  group('Food database', () {
    test('GET /nutrition/food-database returns FoodItemModel list', () async {
      t.adapter.onGet(
        ApiEndpoints.foodDatabase,
        respond: (_) => t.adapter.ok([Samples.foodItem()]),
      );

      final items = await t.apiClient.get<List<FoodItemModel>>(
        ApiEndpoints.foodDatabase,
        parser: (data) => (data as List<dynamic>)
            .map((e) =>
                FoodItemModel.fromFoodDatabaseJson(e as Map<String, dynamic>))
            .toList(),
      );

      expect(t.onlyRequest.path, '/api/v1/nutrition/food-database');
      expect(items.single.name, 'Ful medames');
    });
  });

  group('ClientNutritionViewScreen', () {
    test('GET /nutrition/clients/{clientId}/food-log returns log', () async {
      t.adapter.onGet(
        ApiEndpoints.clientFoodLog(kClientId),
        respond: (_) => t.adapter.ok({
          'entries': [],
          'totalCalories': 0,
        }),
      );

      final data = await t.apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.clientFoodLog(kClientId),
        parser: (d) => d as Map<String, dynamic>,
      );

      expect(t.onlyRequest.path,
          '/api/v1/nutrition/clients/$kClientId/food-log');
      expect(data['totalCalories'], 0);
    });
  });
}
