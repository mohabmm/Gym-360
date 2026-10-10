import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/nutrition/data/datasources/nutrition_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Nutrition screens — food log, water log, food DB search, active meal plan.
void main() {
  late TestApiClient t;
  late NutritionRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = NutritionRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET /nutrition/food-log for a date', () async {
    t.adapter.onGet(
      ApiEndpoints.foodLog,
      respond: (_) => t.adapter.ok(Samples.foodLog()),
    );
    final log = await ds.getFoodLog(
      userId: 'user-1',
      gymId: 'gym-1',
      date: DateTime.utc(2026, 4, 16),
    );
    expect(log.totalCalories, 320);
    expect(log.entries.first.mealType, 'Breakfast');
    expect(t.onlyRequest.q('date'), '2026-04-16');
  });

  test('POST /nutrition/food-log returns new entry id', () async {
    t.adapter.onPost(
      ApiEndpoints.foodLog,
      respond: (_) => t.adapter.created('fle-new'),
    );
    final id = await ds.addFoodEntry({
      'userId': 'user-1',
      'gymId': 'gym-1',
      'date': '2026-04-16',
      'mealType': 'Lunch',
      'foodItems': [
        {'foodItemId': 'food-1', 'quantity': 150.0, 'unit': 'g'},
      ],
    });
    expect(id, 'fle-new');
    expect((t.onlyRequest.bodyAsMap!['foodItems'] as List).first['quantity'],
        150.0);
  });

  test('DELETE /nutrition/food-log/meals/{mealId}/items/{index} — 404 surfaces',
      () async {
    t.adapter.onDelete(
      '${ApiEndpoints.foodLog}/meals/meal-1/items/0',
      respond: (_) => t.adapter.error(404, code: 'NOT_FOUND'),
    );
    await expectLater(
      ds.deleteFoodEntry('meal-1:0'),
      throwsA(isA<NotFoundException>()),
    );
  });

  test('GET /nutrition/water-log for a date', () async {
    t.adapter.onGet(
      ApiEndpoints.waterLog,
      respond: (_) => t.adapter.ok(Samples.waterLog()),
    );
    final log = await ds.getWaterLog(
      userId: 'user-1',
      gymId: 'gym-1',
      date: DateTime.utc(2026, 4, 16),
    );
    expect(log.targetMl, 3000);
  });

  test('POST /nutrition/water-log sends amountMl + YYYY-MM-DD date', () async {
    t.adapter.onPost(
      ApiEndpoints.waterLog,
      respond: (_) => t.adapter.created('we-new'),
    );
    await ds.addWaterEntry(
      userId: 'user-1',
      gymId: 'gym-1',
      date: DateTime.utc(2026, 4, 6),
      amountMl: 500,
    );
    expect(t.onlyRequest.bodyAsMap, {
      'userId': 'user-1',
      'gymId': 'gym-1',
      'date': '2026-04-06',
      'amountMl': 500,
    });
  });

  test('GET /nutrition/me/meal-plan returns plan when assigned', () async {
    t.adapter.onGet(
      ApiEndpoints.myMealPlan,
      respond: (_) => t.adapter.ok(Samples.mealPlan()),
    );
    final plan = await ds.getActiveMealPlan();
    expect(plan, isNotNull);
    expect(plan!.assignedByTrainerId, 'trainer-1');
    expect(plan.dailyCalorieTarget, 2000);
  });

  test('GET /nutrition/me/meal-plan returns null when BE returns empty envelope',
      () async {
    t.adapter.onGet(
      ApiEndpoints.myMealPlan,
      respond: (_) => t.adapter.ok(null),
    );
    expect(await ds.getActiveMealPlan(), isNull);
  });

  test('GET /nutrition/food-database passes query + paging', () async {
    t.adapter.onGet(
      ApiEndpoints.foodDatabase,
      respond: (_) => t.adapter.ok([Samples.foodItem()]),
    );
    final items = await ds.searchFoodItems('ful', page: 2, pageSize: 10);
    expect(items, hasLength(1));
    expect(items.first.nameAr, 'فول مدمس');
    expect(t.onlyRequest.q('query'), 'ful');
    expect(t.onlyRequest.q('page'), '2');
    expect(t.onlyRequest.q('pageSize'), '10');
  });

  test('GET /nutrition/food-database also accepts {items: [...]} shape',
      () async {
    t.adapter.onGet(
      ApiEndpoints.foodDatabase,
      respond: (_) => t.adapter.ok({
        'items': [Samples.foodItem(id: 'food-2')],
      }),
    );
    final items = await ds.searchFoodItems('x');
    expect(items.single.id, 'food-2');
  });
}
