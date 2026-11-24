import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/providers.dart';
import 'package:gymapp_trainee/core/router.dart';
import 'package:gymapp_trainee/features/nutrition/diet/today_meals_card.dart';
import 'package:gymapp_trainee/features/nutrition/presentation/screens/meal_plan_screen.dart';

import 'helpers/test_api_client.dart';

Map<String, dynamic> _meal(String type, String name, double kcal, {String food = 'فول مدمس (Ful medames)'}) => {
      'mealType': type,
      'name': name,
      'description': 'EN $name',
      'foodItems': [
        {'name': food, 'quantity': 150.0, 'unit': 'g', 'calories': kcal, 'protein': 10.0, 'carbs': 20.0, 'fat': 3.0},
        {'name': 'عيش بلدي (Baladi bread (loaf))', 'quantity': 0.5, 'unit': 'pc', 'calories': 105.0},
      ],
      'totalCalories': kcal,
    };

Map<String, dynamic> _today({bool firstDone = false}) => {
      'planId': 'diet-1',
      'planName': 'تخسيس بأكل مصري – بروتين عالي',
      'date': '2026-11-24',
      'targetCalories': 1850.0,
      'targets': {'proteinGrams': 150, 'carbsGrams': 190, 'fatGrams': 50},
      'eatenCalories': firstDone ? 420.0 : 0.0,
      'meals': [
        {'index': 0, 'meal': _meal('Breakfast', 'الفطار: فول وبيض', 420), 'alternative': _meal('Breakfast', 'بديل الفطار: قريش وعيش', 400), 'done': firstDone},
        {'index': 1, 'meal': _meal('Snack', 'سناك: زبادي وبرتقال', 230), 'alternative': null, 'done': false},
      ],
    };

Map<String, dynamic> _plan() => {
      'id': 'diet-1',
      'name': 'تخسيس بأكل مصري – بروتين عالي',
      'description': 'Fat loss on Egyptian food – high protein',
      'dailyCalorieTarget': 1850.00,
      'macroTargets': {'proteinGrams': 150.00, 'carbsGrams': 190.00, 'fatGrams': 50.00},
      'days': [
        for (final day in ['saturday', 'sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday'])
          {
            'dayOfWeek': day,
            'plannedMeals': [_meal('Breakfast', 'الفطار: فول وبيض', 420), _meal('Lunch', 'الغدا: فراخ ورز وسلطة', 650, food: 'فراخ مشوية (Grilled chicken)')],
            'alternatives': [_meal('Breakfast', 'بديل الفطار: قريش وعيش', 400), _meal('Lunch', 'بديل الغدا: كفتة ورز', 640)],
          },
      ],
    };

/// Pumps [child] as the home route; a card is placed in a scrolling page, a screen as it is.
Future<TestApiClient> _pump(WidgetTester tester, Widget child, {String locale = 'ar', bool isScreen = false}) async {
  final t = buildTestApiClient();
  t.adapter.onGet(ApiEndpoints.myMealsToday, respond: (_) => t.adapter.ok(_today()));
  t.adapter.onGet(ApiEndpoints.myMealPlan, respond: (_) => t.adapter.ok(_plan()));
  t.adapter.onPost(ApiEndpoints.myMealDone(0), respond: (_) => t.adapter.ok(_today(firstDone: true)));
  tester.view.physicalSize = const Size(1200, 2600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final router = GoRouter(routes: [
    GoRoute(path: '/', builder: (_, __) => isScreen ? child : Scaffold(body: SingleChildScrollView(child: child))),
    GoRoute(path: AppRoutes.mealPlan, builder: (_, __) => const MealPlanScreen()),
  ]);
  await tester.pumpWidget(ProviderScope(
    overrides: [apiClientProvider.overrideWithValue(t.apiClient)],
    child: MaterialApp.router(
      routerConfig: router,
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
    ),
  ));
  await tester.pumpAndSettle();
  return t;
}

void main() {
  testWidgets('home card: today\'s meals, one tap logs the meal and the count moves', (tester) async {
    final t = await _pump(tester, const TodayMealsCard());

    expect(find.text('أكل النهارده'), findsOneWidget);
    expect(find.text('0 من 2 وجبات · 0 من 1850 سعرة'), findsOneWidget);
    expect(find.text('الكابتن بيتابع معاك في الحصة الجاية'), findsOneWidget, reason: 'the diet leads back to the sessions');

    await tester.tap(find.byKey(const Key('today-meal-ate-0')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('أكلتها ✓').last);
    await tester.pumpAndSettle();

    final post = t.adapter.recorded.lastWhere((r) => r.method == 'POST');
    expect(post.path, ApiEndpoints.myMealDone(0));
    expect(post.bodyAsMap, {'alternative': false});
  });

  testWidgets('the swap can be ticked instead of the meal', (tester) async {
    final t = await _pump(tester, const TodayMealsCard());

    await tester.tap(find.byKey(const Key('today-meal-ate-0')));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('أكلت البديل'));
    await tester.pumpAndSettle();

    expect(t.adapter.recorded.lastWhere((r) => r.method == 'POST').bodyAsMap, {'alternative': true});
  });

  testWidgets('no diet yet: the card is not shown', (tester) async {
    final t = buildTestApiClient();
    t.adapter.onGet(ApiEndpoints.myMealsToday, respond: (_) => t.adapter.ok(null));
    await tester.pumpWidget(ProviderScope(
      overrides: [apiClientProvider.overrideWithValue(t.apiClient)],
      child: const MaterialApp(home: Scaffold(body: TodayMealsCard())),
    ));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('today-meals-card')), findsNothing);
  });

  testWidgets('diet screen: target, macros, a day per chip, foods with portions and the swap', (tester) async {
    await _pump(tester, const MealPlanScreen(), locale: 'en', isScreen: true);

    expect(find.text('Daily target: 1850 kcal'), findsOneWidget);
    expect(find.text('Protein 150 g'), findsOneWidget);
    expect(find.text('Saturday'), findsOneWidget);
    expect(find.text('EN الفطار: فول وبيض'), findsOneWidget);
    expect(find.text('Ful medames'), findsWidgets);
    expect(find.text('150 g'), findsWidgets);
    expect(find.text('× ½'), findsWidgets);
    expect(find.textContaining('Swap: EN بديل الغدا'), findsOneWidget);
  });
}
