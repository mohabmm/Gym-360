import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/clients/presentation/providers/client_providers.dart';
import 'package:gymapp_trainer/features/clients/presentation/screens/record_measurements_screen.dart';

final _today = DateTime.now();

BodyMetricEntryModel _entry(DateTime day, {double? weight, double? waist}) => BodyMetricEntryModel(
      id: apiDate(day),
      userId: 'c1',
      gymId: 'g1',
      date: apiDate(day),
      weight: weight,
      weightUnit: 'Kg',
      measurements: waist == null ? null : BodyMeasurementsModel(waist: waist, measurementUnit: 'Cm'),
      source: 'TrainerMeasured',
      loggedBy: 'coach-1',
      createdAt: day,
    );

Future<void> _pump(
  WidgetTester tester, {
  ProgressGoalModel? goal,
  List<BodyMetricEntryModel> entries = const [],
}) async {
  tester.view.physicalSize = const Size(1200, 4000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      clientJourneyProvider('c1').overrideWith(
        (ref) async => ProgressJourney.from(ProgressJourneyModel(userId: 'c1', entries: entries, goal: goal)),
      ),
    ],
    child: MaterialApp(
      locale: const Locale('en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: Builder(
        builder: (context) => Scaffold(
          body: Center(
            child: TextButton(
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute<void>(builder: (_) => const RecordMeasurementsScreen(clientId: 'c1')),
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    ),
  ));
  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
}

Finder _field(ProgressMetric m) => find.byKey(ValueKey('metric-${m.name}'));

ButtonStyleButton _saveButton(WidgetTester tester) =>
    tester.widget<ButtonStyleButton>(find.byKey(const ValueKey('save-measurements')));

void main() {
  _goalAndLayoutTests();
  _validationTests();
}

void _goalAndLayoutTests() {
  testWidgets('first session (no goal yet): coach picks a goal, then a target field appears', (tester) async {
    await _pump(tester);

    expect(find.text('Goal'), findsOneWidget);
    expect(find.text('Target weight (kg)'), findsNothing);
    expect(find.text('First check-in — this becomes Day 1'), findsOneWidget);
    await tester.tap(find.widgetWithText(ChoiceChip, 'Lose fat'));
    await tester.pumpAndSettle();
    expect(find.text('Target weight (kg)'), findsOneWidget);
  });

  testWidgets('client already has a goal: the goal step is not shown', (tester) async {
    await _pump(tester, goal: const ProgressGoalModel(goal: ProgressGoalType.buildMuscle));

    expect(find.widgetWithText(ChoiceChip, 'Lose fat'), findsNothing);
    expect(find.text('Weight'), findsOneWidget);
  });

  testWidgets('every tape measurement the API supports is offered, arms/thighs/calves as left|right', (tester) async {
    await _pump(tester);

    for (final label in ['Chest', 'Waist', 'Hips', 'Neck', 'Arms', 'Thighs', 'Calves']) {
      expect(find.text(label), findsOneWidget, reason: label);
    }
    expect(find.text('Left'), findsNWidgets(3));
    expect(_field(ProgressMetric.leftCalf), findsOneWidget);
  });

  testWidgets('shows the last reading and a live change colored by the goal', (tester) async {
    await _pump(
      tester,
      goal: const ProgressGoalModel(goal: ProgressGoalType.loseFat),
      entries: [_entry(_today.subtract(const Duration(days: 14)), weight: 85, waist: 92)],
    );

    expect(find.textContaining('Last: 85 kg'), findsOneWidget);
    expect(find.textContaining('Last check-in:'), findsOneWidget);
    await tester.enterText(_field(ProgressMetric.weight), '83.5');
    await tester.pump();

    expect(find.text('↓ 1.5 kg'), findsOneWidget);
    final chip = tester.widget<ChangeChip>(find.byType(ChangeChip));
    expect(chip.tone, ChangeTone.good);
    expect(find.text('Save 1'), findsOneWidget);
  });

  testWidgets('out-of-range value shows the allowed range and blocks Save', (tester) async {
    await _pump(tester);

    await tester.enterText(_field(ProgressMetric.weight), '820');
    await tester.pump();

    expect(find.text('Enter 20–350'), findsOneWidget);
    expect(find.text('Fix the values marked in red'), findsOneWidget);
    expect(_saveButton(tester).onPressed, isNull);
  });
}

void _validationTests() {
  testWidgets('big jump from the last reading warns but still allows Save', (tester) async {
    await _pump(tester, entries: [_entry(_today.subtract(const Duration(days: 7)), waist: 90)]);

    await tester.enterText(_field(ProgressMetric.waist), '60');
    await tester.pump();

    expect(find.text('Big change since last time — double-check'), findsOneWidget);
    expect(_saveButton(tester).onPressed, isNotNull);
  });

  testWidgets('Arabic-Indic digits and comma decimals are converted as typed', (tester) async {
    await _pump(tester);

    await tester.enterText(_field(ProgressMetric.weight), '٨٢٫٥');
    await tester.pump();

    expect(find.widgetWithText(TextField, '82.5'), findsOneWidget);
  });

  testWidgets('a check-in already on today blocks Save before the coach types anything', (tester) async {
    await _pump(tester, entries: [_entry(_today, weight: 80)]);

    expect(find.text('A check-in already exists on this day. Pick another date.'), findsNWidgets(2));
    await tester.enterText(_field(ProgressMetric.weight), '79');
    await tester.pump();
    expect(_saveButton(tester).onPressed, isNull);
  });

  testWidgets('leaving with unsaved values asks before discarding', (tester) async {
    await _pump(tester);

    await tester.enterText(_field(ProgressMetric.weight), '80');
    await tester.pump();
    await tester.pageBack();
    await tester.pumpAndSettle();

    expect(find.text('Discard measurements?'), findsOneWidget);
    await tester.tap(find.text('Discard'));
    await tester.pumpAndSettle();
    expect(find.text('open'), findsOneWidget);
  });
}
