import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/plans/data/session_plan_days.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/exercise_progress_providers.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_draft.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_screen.dart';

import 'exercise_progress_fixtures.dart';

/// The member's plan: week 1 Push / Legs, week 2 Push B.
const _push = PlanDayModel(key: 'w1d1', title: 'Push', titleAr: 'دفع', exercises: [
  PlanExerciseModel(key: 'e1', name: 'Bench press', sets: 3, reps: '8-10', targetWeightKg: 40),
  PlanExerciseModel(key: 'e2', name: 'Plank', sets: 2, seconds: 45),
]);
const _legs = PlanDayModel(key: 'w1d2', title: 'Legs', exercises: [
  PlanExerciseModel(key: 'e3', name: 'Leg press', sets: 4, reps: '12', targetWeightKg: 90),
]);
const _pushB = PlanDayModel(key: 'w2d1', title: 'Push B', exercises: [
  PlanExerciseModel(key: 'e4', name: 'Incline press', sets: 3, reps: '10'),
]);

SessionPlanDays _plan({String? suggested}) => SessionPlanDays(
      planId: 'p1',
      slots: PlanSlot.of(const PlanDocumentModel(weeks: [
        PlanWeekModel(week: 1, days: [_push, _legs]),
        PlanWeekModel(week: 2, days: [_pushB]),
      ])),
      suggestedKey: suggested,
    );

Future<void> _pump(WidgetTester tester, {SessionPlanDays? plan, String? planDayKey, String locale = 'en'}) async {
  tester.view.physicalSize = const Size(1080, 4000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      clientExerciseProgressProvider('c1').overrideWith((ref) async => trialBoard()),
      sessionPlanDaysProvider('c1').overrideWith((ref) async => plan),
    ],
    child: MaterialApp(
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: SessionExercisesScreen(sessionId: 's9', clientId: 'c1', planDayKey: planDayKey),
    ),
  ));
  await tester.pumpAndSettle();
}

Finder _name(String name) => find.widgetWithText(TextFormField, name);

ExerciseSource _selected(WidgetTester tester) =>
    tester.widget<SegmentedButton<ExerciseSource>>(find.byKey(const ValueKey('exercise-source'))).selected.single;

void main() {
  group('draft', _draftTests);
  group('opening', _openingTests);
  group('switching', _switchingTests);
}

void _openingTests() {
  testWidgets("with a plan the exercises open on the member's next plan day, filled in", (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d2'));

    expect(_selected(tester), ExerciseSource.plan);
    expect(_name('Leg press'), findsOneWidget);
    expect(find.text('90'), findsNWidgets(4), reason: 'every planned set carries the plan weight');
    final chip = tester.widget<ChoiceChip>(find.byKey(const ValueKey('plan-exercises-day-w1d2')));
    expect(chip.selected, isTrue);
  });

  testWidgets('the plan day counted when recording wins over the next day', (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d2'), planDayKey: 'w1d1');

    expect(_name('Bench press'), findsOneWidget);
    expect(_name('Plank'), findsOneWidget);
    expect(_name('Leg press'), findsNothing);
  });

  testWidgets('no plan: no source switch, the screen is as before', (tester) async {
    await _pump(tester);

    expect(find.byKey(const ValueKey('exercise-source')), findsNothing);
    expect(find.text('Which training day?'), findsOneWidget);
  });

  testWidgets('Arabic: Arabic labels and the Arabic plan day title', (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d1'), locale: 'ar');

    expect(find.text('من خطة العضو'), findsOneWidget);
    expect(find.text('من البداية'), findsOneWidget);
    expect(find.text('دفع'), findsOneWidget);
  });
}

void _switchingTests() {
  testWidgets('chose the plan by mistake: From scratch empties the form, Undo brings the plan back', (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d1'));

    await tester.tap(find.byKey(const ValueKey('source-scratch')));
    await tester.pumpAndSettle();
    expect(_selected(tester), ExerciseSource.scratch);
    expect(_name('Bench press'), findsNothing);
    expect(find.text('Which training day?'), findsOneWidget, reason: 'scratch shows the usual day labels');

    await tester.tap(find.byKey(const ValueKey('undo-exercises')));
    await tester.pumpAndSettle();
    expect(_selected(tester), ExerciseSource.plan);
    expect(_name('Bench press'), findsOneWidget);
  });

  testWidgets("switching back to the member's plan refills the plan day", (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d1'));
    await tester.tap(find.byKey(const ValueKey('source-scratch')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('source-plan')));
    await tester.pumpAndSettle();

    expect(_selected(tester), ExerciseSource.plan);
    expect(_name('Bench press'), findsOneWidget);
    expect(find.textContaining('from the member\'s plan'), findsOneWidget);
  });

  testWidgets('another plan day swaps an untouched template; other weeks are a dropdown away', (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d1'));

    await tester.tap(find.byKey(const ValueKey('plan-exercises-day-w1d2')));
    await tester.pumpAndSettle();
    expect(_name('Leg press'), findsOneWidget);
    expect(_name('Bench press'), findsNothing);

    await tester.tap(find.byKey(const ValueKey('plan-week')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Week 2').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('plan-exercises-day-w2d1')));
    await tester.pumpAndSettle();
    expect(_name('Incline press'), findsOneWidget);
  });

  testWidgets("the coach's own exercises are replaced by the plan only after asking", (tester) async {
    await _pump(tester, plan: _plan(suggested: 'w1d1'));
    await tester.tap(find.byKey(const ValueKey('source-scratch')));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Exercise'), 'Squat');
    await tester.pump();

    await tester.tap(find.byKey(const ValueKey('source-plan')));
    await tester.pumpAndSettle();
    expect(find.text('Replace exercises?'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('keep-mine')));
    await tester.pumpAndSettle();
    expect(_name('Squat'), findsOneWidget);
    expect(_name('Bench press'), findsNothing);

    await tester.tap(find.byKey(const ValueKey('plan-exercises-day-w1d1')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('replace-exercises')));
    await tester.pumpAndSettle();
    expect(_name('Bench press'), findsOneWidget);
    expect(_name('Squat'), findsNothing);
  });
}

void _draftTests() {
  test('a planned exercise becomes sets with the plan reps (range = lower bound) and weight', () {
    final row = ExerciseRowDraft.fromPlan(_push.exercises.first);

    expect(row.name, 'Bench press');
    expect(row.measure, ExerciseMeasure.reps);
    expect(row.sets.map((s) => s.reps), ['8', '8', '8']);
    expect(row.sets.map((s) => s.weight), [40, 40, 40]);
    expect(row.isValid, isTrue, reason: 'ready to save as planned');
  });

  test('a timed planned exercise is counted in time', () {
    final row = ExerciseRowDraft.fromPlan(_push.exercises.last);

    expect(row.measure, ExerciseMeasure.time);
    expect(row.sets.map((s) => s.seconds), [45, 45]);
  });

  test('picking a plan day names the session after it and fills its exercises', () {
    final draft = SessionExercisesDraft();

    expect(draft.pickPlanDay(_push, isArabic: false), DayPick.filled);
    expect(draft.dayLabel, 'Push');
    expect(draft.rows.map((r) => r.name), ['Bench press', 'Plank']);
    expect(draft.isUnchangedPrefill, isTrue);
  });

  test("the coach's own rows are kept unless they agree", () {
    final draft = SessionExercisesDraft();
    draft.rows.first.name = 'Squat';

    expect(draft.pickPlanDay(_push, isArabic: false), DayPick.needsConfirm);
    expect(draft.rows.single.name, 'Squat');
    expect(draft.pickPlanDay(_push, isArabic: false, replace: true), DayPick.filled);
  });

  test('a long plan day title is cut to what the API keeps', () {
    final long = PlanDayModel(key: 'k', title: 'x' * 60);

    expect(SessionExercisesDraft.planDayLabel(long, false).length, SessionExercisesDraft.maxDayLabelLength);
  });
}
