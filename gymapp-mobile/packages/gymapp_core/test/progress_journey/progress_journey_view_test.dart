import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'journey_fixtures.dart';

final _now = DateTime(2026, 11, 13);

Future<void> pump(WidgetTester tester, Widget child, {bool arabic = false, double width = 432}) async {
  tester.view.physicalSize = Size(width * 2.5, 10000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    locale: Locale(arabic ? 'ar' : 'en'),
    supportedLocales: const [Locale('ar'), Locale('en')],
    localizationsDelegates: GlobalMaterialLocalizations.delegates,
    home: Scaffold(body: SingleChildScrollView(child: child)),
  ));
  await tester.pumpAndSettle();
}

ProgressJourney threeCheckIns({ProgressGoalType? goal = ProgressGoalType.loseFat}) => journeyOf([
      entry('2026-10-10', {ProgressMetric.weight: 95, ProgressMetric.waist: 104}),
      weighIn('2026-10-25', 93, self: true),
      entry('2026-11-10', {ProgressMetric.weight: 91, ProgressMetric.waist: 99}),
    ], goal: goal, target: 85);

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  group('layout (EN, AR, small phone)', _layoutTests);
  group('interaction and states', _stateTests);
  group('coach view, home card and goal picker', _extraTests);
}

void _layoutTests() {
  testWidgets('member: hero, chart, day 1 vs now and named coach check-ins', (tester) async {
    await pump(
      tester,
      ProgressJourneyView(
        journey: threeCheckIns(),
        isArabic: false,
        viewer: const MemberViewer(coachId: 'coach-1', coachName: 'Ahmed'),
        now: _now,
      ),
    );

    expect(find.text('Since day one'), findsOneWidget);
    expect(find.textContaining('34 days ago'), findsOneWidget);
    expect(find.text('Lose fat'), findsOneWidget);
    expect(find.text('↓ 4 kg'), findsWidgets);
    expect(find.text('↓ 5 cm'), findsOneWidget);
    expect(find.text('Day 1 vs now'), findsOneWidget);
    expect(find.textContaining('of the way to 85 kg'), findsOneWidget);
    expect(find.byType(LineChart), findsOneWidget);
    expect(find.text('Coach Ahmed'), findsNWidgets(2));
    expect(find.text('You'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('member in Arabic renders right-to-left without overflow', (tester) async {
    await pump(
      tester,
      ProgressJourneyView(journey: threeCheckIns(), isArabic: true, viewer: const MemberViewer(), now: _now),
      arabic: true,
    );

    expect(find.text('منذ اليوم الأول'), findsOneWidget);
    expect(find.text('خسارة الدهون'), findsOneWidget);
    expect(find.text('اليوم الأول مقابل الآن'), findsOneWidget);
    expect(find.text('المدرب'), findsNWidgets(2));
    final dir = tester.widget<Directionality>(find.byType(Directionality).first);
    expect(dir.textDirection, TextDirection.rtl);
    expect(tester.takeException(), isNull);
  });

  testWidgets('small phone (320dp) in Arabic with every section has no overflow', (tester) async {
    final journey = journeyOf([
      entry('2026-08-01', {ProgressMetric.weight: 102.5, ProgressMetric.waist: 110.5, ProgressMetric.leftArm: 33.5}),
      entry('2026-11-10', {ProgressMetric.weight: 96.5, ProgressMetric.waist: 101.5, ProgressMetric.leftArm: 35.5}),
    ], goal: ProgressGoalType.buildMuscle, target: 90);
    await pump(
      tester,
      ProgressJourneyView(
        journey: journey,
        isArabic: true,
        viewer: const MemberViewer(coachId: 'coach-1', coachName: 'عبد الرحمن'),
        onEditGoal: () {},
        now: _now,
      ),
      arabic: true,
      width: 320,
    );

    expect(find.text('الكابتن عبد الرحمن'), findsNWidgets(2));
    expect(tester.takeException(), isNull);
  });
}

void _stateTests() {
  testWidgets('switching the metric chip and range keeps the chart', (tester) async {
    await pump(
      tester,
      ProgressJourneyView(journey: threeCheckIns(), isArabic: false, viewer: const MemberViewer(), now: _now),
    );

    await tester.tap(find.widgetWithText(ChoiceChip, 'Waist'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('1M'));
    await tester.pumpAndSettle();

    expect(find.byType(LineChart), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('single check-in shows the baseline message and no comparison table', (tester) async {
    await pump(
      tester,
      ProgressJourneyView(
        journey: journeyOf([weighIn('2026-11-10', 95)]),
        isArabic: false,
        viewer: const MemberViewer(),
        now: _now,
      ),
    );

    expect(find.text('Your baseline is set'), findsOneWidget);
    expect(find.text('Day 1 vs now'), findsNothing);
    expect(find.text('Day 1'), findsOneWidget, reason: 'timeline badge');
  });

  testWidgets('no goal + edit callback shows "Set a goal" and calls back', (tester) async {
    var tapped = false;
    await pump(
      tester,
      ProgressJourneyView(
        journey: threeCheckIns(goal: null),
        isArabic: false,
        viewer: const MemberViewer(),
        onEditGoal: () => tapped = true,
        now: _now,
      ),
    );

    await tester.tap(find.text('Set a goal'));
    expect(tapped, isTrue);
  });

  testWidgets('empty: member is told the coach measures in the first session', (tester) async {
    await pump(tester, ProgressJourneyView(journey: journeyOf([]), isArabic: false, viewer: const MemberViewer()));

    expect(find.textContaining('first measurements in your first session'), findsOneWidget);
    expect(find.text('Record measurements'), findsNothing);
  });
}

void _extraTests() {
  testWidgets('empty: coach gets a record-measurements action; own entries show as You', (tester) async {
    var recorded = false;
    await pump(
      tester,
      ProgressJourneyView(
        journey: journeyOf([]),
        isArabic: false,
        viewer: const CoachViewer(coachId: 'coach-1'),
        onRecordMeasurements: () => recorded = true,
      ),
    );
    await tester.tap(find.text('Record measurements'));
    expect(recorded, isTrue);

    await pump(
      tester,
      ProgressJourneyView(journey: threeCheckIns(), isArabic: false, viewer: const CoachViewer(coachId: 'coach-1'), now: _now),
    );
    expect(find.text('You'), findsNWidgets(2));
    expect(find.text('Client'), findsOneWidget);
  });

  testWidgets('home card hides until there is a change, then shows it', (tester) async {
    await pump(tester, SinceDayOneCard(journey: journeyOf([weighIn('2026-11-10', 95)]), isArabic: false));
    expect(find.byType(AppCard), findsNothing);

    await pump(tester, SinceDayOneCard(journey: threeCheckIns(), isArabic: false, now: _now));
    expect(find.text('Since day one'), findsOneWidget);
    expect(find.text('↓ 4 kg'), findsOneWidget);
  });

  testWidgets('goal picker returns the goal and target, and rejects an impossible target', (tester) async {
    GoalSelection? picked;
    await pump(
      tester,
      Builder(
        builder: (context) => TextButton(
          onPressed: () async =>
              picked = await showGoalPicker(context, strings: const ProgressJourneyStrings(false)),
          child: const Text('open'),
        ),
      ),
    );

    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Build muscle'));
    await tester.enterText(find.byType(TextField), '0');
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(find.text('Enter a weight between 1 and 500'), findsOneWidget);

    await tester.enterText(find.byType(TextField), '78,5');
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(picked?.goal, ProgressGoalType.buildMuscle);
    expect(picked?.targetWeight, 78.5);
  });
}
