import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/exercise_progress_providers.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_screen.dart';

import 'exercise_progress_fixtures.dart';

Future<void> _pump(WidgetTester tester, {String locale = 'en'}) async {
  tester.view.physicalSize = const Size(1080, 4000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [clientExerciseProgressProvider('c1').overrideWith((ref) async => trialBoard())],
    child: MaterialApp(
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: const SessionExercisesScreen(sessionId: 's9', clientId: 'c1'),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('save stays off until an exercise has a name and reps', (tester) async {
    await _pump(tester);

    FilledButton save() => tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Save exercises'));
    expect(save().onPressed, isNull);

    await tester.enterText(find.widgetWithText(TextFormField, 'Exercise'), 'Squat');
    await tester.enterText(find.widgetWithText(TextFormField, 'Reps (10,8,8)'), '5');
    await tester.pump();

    expect(save().onPressed, isNotNull);
  });

  testWidgets('picking Push Day fills last Push Day with the coach target', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Push Day'));
    await tester.pumpAndSettle();

    expect(find.text('Bench press'), findsOneWidget);
    expect(find.text('65'), findsOneWidget);
    expect(find.textContaining("Filled in last Push Day's exercises"), findsOneWidget);
  });

  testWidgets('next target fields open on demand', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Next session target'));
    await tester.pumpAndSettle();

    expect(find.widgetWithText(TextFormField, 'kg'), findsNWidgets(2));
  });

  testWidgets('Arabic: right-to-left with Arabic day names', (tester) async {
    await _pump(tester, locale: 'ar');

    expect(find.text('يوم الدفع'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('يوم الدفع'))), TextDirection.rtl);
    expect(tester.takeException(), isNull);
  });
}
