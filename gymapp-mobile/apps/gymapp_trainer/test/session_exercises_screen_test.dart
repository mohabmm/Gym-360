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
  _perSetTests();

  testWidgets('save is off on an empty form and on once something is typed', (tester) async {
    await _pump(tester);

    FilledButton save() => tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Save exercises'));
    expect(save().onPressed, isNull);

    await tester.enterText(find.widgetWithText(TextFormField, 'Exercise'), 'Squat');
    await tester.pump();

    expect(save().onPressed, isNotNull);
  });

  testWidgets('picking Push Day fills last Push Day with the coach target', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Push Day'));
    await tester.pumpAndSettle();

    expect(find.text('Bench press'), findsOneWidget);
    expect(find.text('65'), findsNWidgets(3), reason: 'the target weight fills every set');
    expect(find.textContaining("Filled in last Push Day's exercises"), findsOneWidget);
  });

  testWidgets('switch day, untick it, start over and undo — all without leaving the screen', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Push Day'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Pull Day'));
    await tester.pumpAndSettle();
    expect(find.text('Barbell row'), findsOneWidget, reason: 'the untouched Push template is swapped');
    expect(find.text('Bench press'), findsNothing);

    await tester.tap(find.text('Pull Day'));
    await tester.pumpAndSettle();
    expect(find.text('Barbell row'), findsNothing, reason: 'no day = no template');

    await tester.tap(find.byKey(const ValueKey('undo-exercises')));
    await tester.pumpAndSettle();
    expect(find.text('Barbell row'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('start-empty')));
    await tester.pumpAndSettle();
    expect(find.text('Barbell row'), findsNothing);
    expect(find.byKey(const ValueKey('start-empty')), findsNothing);
  });

  testWidgets("the coach's own exercises are replaced only after asking", (tester) async {
    await _pump(tester);

    await tester.enterText(find.widgetWithText(TextFormField, 'Exercise'), 'Squat');
    await tester.pump();
    await tester.tap(find.text('Push Day'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('keep-mine')));
    await tester.pumpAndSettle();
    expect(find.text('Squat'), findsOneWidget);
    expect(tester.widget<ChoiceChip>(find.widgetWithText(ChoiceChip, 'Push Day')).selected, isTrue);

    await tester.tap(find.text('Pull Day'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('replace-exercises')));
    await tester.pumpAndSettle();
    expect(find.text('Barbell row'), findsOneWidget);
    expect(find.text('Squat'), findsNothing);
  });

  testWidgets('next target fields open on demand', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Next session target'));
    await tester.pumpAndSettle();

    expect(find.widgetWithText(TextFormField, 'Weight (kg)'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Reps (e.g. 8-12)'), findsOneWidget);
  });

  testWidgets('Arabic: right-to-left with Arabic day names', (tester) async {
    await _pump(tester, locale: 'ar');

    expect(find.text('يوم الدفع'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('يوم الدفع'))), TextDirection.rtl);
    expect(tester.takeException(), isNull);
  });
}

/// Weight per set, timed sets and the save checks that name the missing set.
void _perSetTests() {
  testWidgets('one line per set; + repeats the last set with its kg', (tester) async {
    await _pump(tester);

    expect(find.text('3'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Set 3 reps'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Set 4 reps'), findsNothing);

    await tester.enterText(find.widgetWithText(TextFormField, 'Set 3 reps'), '8');
    await tester.enterText(find.widgetWithText(TextFormField, 'Set 3 kg'), '80');
    await tester.tap(find.byTooltip('One more set'));
    await tester.pump();

    Finder inSet4(String label, String value) =>
        find.descendant(of: find.widgetWithText(TextFormField, label), matching: find.text(value));
    expect(find.text('4'), findsOneWidget);
    expect(inSet4('Set 4 reps', '8'), findsOneWidget);
    expect(inSet4('Set 4 kg', '80'), findsOneWidget);
  });

  testWidgets('every set has its own coach note; a new set starts with an empty one', (tester) async {
    await _pump(tester);

    for (final n in [1, 2, 3]) {
      expect(find.widgetWithText(TextFormField, 'Your note on set $n'), findsOneWidget);
    }
    await tester.enterText(find.widgetWithText(TextFormField, 'Your note on set 3'), 'Last 2 reps assisted');
    await tester.tap(find.byTooltip('One more set'));
    await tester.pump();

    expect(find.text('Last 2 reps assisted'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Your note on set 4'), findsOneWidget);
  });

  testWidgets('switching to Time asks for each set\'s time with optional kg', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Time'));
    await tester.pump();

    expect(find.widgetWithText(TextFormField, 'Set 1 time (s or m:ss)'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'kg (optional)'), findsNWidgets(3));
    expect(find.widgetWithText(TextFormField, 'Set 1 reps'), findsNothing);

    await tester.enterText(find.widgetWithText(TextFormField, 'Exercise'), 'Plank');
    await tester.enterText(find.widgetWithText(TextFormField, 'Set 1 time (s or m:ss)'), '45');
    await tester.tap(find.widgetWithText(FilledButton, 'Save exercises'));
    await tester.pumpAndSettle();

    expect(find.text('Fill in the time for set 2, 3 of "Plank"'), findsOneWidget);
  });

  testWidgets('saving with a set missing reps says which set and marks it', (tester) async {
    await _pump(tester);

    await tester.enterText(find.widgetWithText(TextFormField, 'Exercise'), 'Squat');
    await tester.enterText(find.widgetWithText(TextFormField, 'Set 1 reps'), '10');
    await tester.enterText(find.widgetWithText(TextFormField, 'Set 2 reps'), '10');
    await tester.enterText(find.widgetWithText(TextFormField, 'Set 3 kg'), '60');
    await tester.tap(find.widgetWithText(FilledButton, 'Save exercises'));
    await tester.pumpAndSettle();

    expect(find.text('Fill in the reps for set 3 of "Squat"'), findsOneWidget);
    expect(find.text('Required'), findsOneWidget);
  });
}
