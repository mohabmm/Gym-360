import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/features/nutrition/diet/client_diet_tab.dart';
import 'package:gymapp_trainer/features/nutrition/diet/diet_builder_screen.dart';

import 'helpers/test_api_client.dart';

late TestApiClient t;

Map<String, dynamic> _meal(String name, double kcal) => {
      'mealType': 'Breakfast',
      'name': name,
      'description': 'EN $name',
      'foodItems': [
        {'name': 'فول مدمس (Ful medames)', 'quantity': 150.0, 'unit': 'g', 'calories': kcal},
      ],
      'totalCalories': kcal,
    };

Map<String, dynamic> _diet(String key, String nameAr, String? goal) => {
      'key': key,
      'name': 'EN $key',
      'nameAr': nameAr,
      'description': 'desc',
      'descriptionAr': 'وصف',
      'goal': goal,
      'mealsPerDay': 5,
    };

Future<void> _pump(WidgetTester tester, Widget child) async {
  tester.view.physicalSize = const Size(1200, 2800);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [apiClientProvider.overrideWithValue(t.apiClient)],
    child: MaterialApp(
      locale: const Locale('ar'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: Scaffold(body: child),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  setUp(() {
    t = buildTestApiClient();
    t.adapter.onPost(ApiEndpoints.calorieTarget, respond: (_) => t.adapter.ok({
          'bmr': 1450, 'maintenance': 2000, 'calories': 1600, 'proteinGrams': 140, 'carbsGrams': 155, 'fatGrams': 45, 'goal': 'FatLoss',
        }));
    t.adapter.onGet(ApiEndpoints.readyDiets, respond: (_) => t.adapter.ok([
          _diet('fat_loss_high_protein', 'تخسيس بأكل مصري – بروتين عالي', 'FatLoss'),
          _diet('ramadan', 'رمضان – فطار وسحور', null),
        ]));
    t.adapter.onGet(ApiEndpoints.readyDietByKey('fat_loss_high_protein'), respond: (_) => t.adapter.ok({
          'summary': _diet('fat_loss_high_protein', 'تخسيس', 'FatLoss'),
          'calories': 1600,
          'days': [
            {'dayOfWeek': 'saturday', 'plannedMeals': [_meal('الفطار: فول وبيض', 380)], 'alternatives': []},
          ],
        }));
    t.adapter.onPost(ApiEndpoints.clientDiet('client-1'), respond: (_) => t.adapter.ok({'id': 'plan-1'}));
  });

  testWidgets('prefilled calculator → target → the goal\'s diet → give: the scaled target is sent', (tester) async {
    await _pump(tester, const DietBuilderScreen(
      clientId: 'client-1',
      prefill: DietPrefill(sex: DietSex.female, age: 28, weightKg: 72, goal: DietGoal.fatLoss),
    ));

    expect(find.widgetWithText(TextField, '28'), findsOneWidget, reason: 'age from the profile');
    expect(find.widgetWithText(TextField, '72'), findsOneWidget, reason: 'weight from the last measurement');
    await tester.enterText(find.byKey(const Key('diet-height')), '165');
    await tester.tap(find.byKey(const Key('diet-calculate')));
    await tester.pumpAndSettle();

    final calc = t.adapter.recorded.lastWhere((r) => r.path == ApiEndpoints.calorieTarget).bodyAsMap!;
    expect(calc, {'sex': 'Female', 'age': 28, 'heightCm': 165.0, 'weightKg': 72.0, 'activity': 'Light', 'goal': 'FatLoss'});
    expect(find.text('هدفك اليومي: 1600 سعرة'), findsOneWidget);
    expect(find.textContaining('الحرق وقت الراحة 1450'), findsOneWidget);

    await tester.tap(find.byKey(const Key('diet-fat_loss_high_protein')));
    await tester.pumpAndSettle();
    expect(find.textContaining('شكل يوم في الخطة'), findsOneWidget);
    expect(find.textContaining('فول مدمس 150 جم'), findsOneWidget);

    await tester.tap(find.byKey(const Key('diet-give')));
    await tester.pumpAndSettle();

    final give = t.adapter.recorded.lastWhere((r) => r.path == ApiEndpoints.clientDiet('client-1') && r.method == 'POST').bodyAsMap!;
    expect(give['dietKey'], 'fat_loss_high_protein');
    expect(give['target'], {'calories': 1600, 'proteinGrams': 140, 'carbsGrams': 155, 'fatGrams': 45});
  });

  testWidgets('without height the coach is asked to fill it in and nothing is sent', (tester) async {
    await _pump(tester, const DietBuilderScreen(clientId: 'client-1', prefill: DietPrefill()));

    await tester.tap(find.byKey(const Key('diet-calculate')));
    await tester.pump();

    expect(find.text('اكمل السن والطول والوزن'), findsOneWidget);
    expect(t.adapter.recorded.where((r) => r.path == ApiEndpoints.calorieTarget), isEmpty);
  });

  testWidgets('client tab: the diet, the week of ticks to talk about at the session', (tester) async {
    t.adapter.onGet(ApiEndpoints.clientDiet('client-1'), respond: (_) => t.adapter.ok({
          'plan': {
            'id': 'plan-1',
            'name': 'تخسيس بأكل مصري – بروتين عالي',
            'dailyCalorieTarget': 1600.00,
            'macroTargets': {'proteinGrams': 140.00, 'carbsGrams': 155.00, 'fatGrams': 45.00},
            'days': [],
          },
          'lastWeek': [
            for (var i = 0; i < 7; i++) {'date': '2026-11-${18 + i}', 'mealsDone': i == 6 ? 3 : 4, 'mealsPlanned': 5},
          ],
        }));
    await _pump(tester, const ClientDietTab(clientId: 'client-1', isArabic: true));

    expect(find.text('تخسيس بأكل مصري – بروتين عالي'), findsOneWidget);
    expect(find.text('آخر ٧ أيام: 27 من 35 وجبة'), findsOneWidget);
    expect(find.text('اتكلم معاه عن الالتزام في الحصة الجاية'), findsOneWidget);
    expect(find.text('خطة أكل جديدة بدلها'), findsOneWidget);
  });
}
