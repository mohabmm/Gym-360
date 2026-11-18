import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'journey_fixtures.dart';

final _now = DateTime(2026, 11, 13);

Future<void> _pump(WidgetTester tester, Widget child, {bool arabic = false, double width = 432}) async {
  tester.view.physicalSize = Size(width * 2.5, 6000);
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

BodyMetricEntryModel _withNote(BodyMetricEntryModel e, String note) => e.copyWith(
      notes: note,
      composition: const BodyCompositionModel(skeletalMuscleMassKg: 34.2, skeletalMusclePercent: 37.6, bmi: 28.1),
    );

ProgressJourney _journey() => journeyOf([
      entry('2026-10-10', {ProgressMetric.weight: 95, ProgressMetric.waist: 104}),
      _withNote(entry('2026-11-11', {ProgressMetric.weight: 92, ProgressMetric.waist: 100}),
          'Great work — keep protein high and sleep 8h.'),
    ], goal: ProgressGoalType.loseFat);

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  group('member: latest check-in and achievements', _latestTests);
  group('full report', _reportTests);
  group('coach view', _coachTests);
}

void _latestTests() {
  testWidgets('member sees the latest coach check-in up front with the note and wins', (tester) async {
    await _pump(
      tester,
      ProgressJourneyView(
        journey: _journey(),
        isArabic: false,
        viewer: const MemberViewer(coachId: 'coach-1', coachName: 'Ahmed'),
        now: _now,
      ),
    );

    expect(find.text('Latest check-in'), findsOneWidget);
    expect(find.text('New'), findsOneWidget, reason: 'measured 2 days ago');
    expect(find.text('2 measurements improved'), findsOneWidget);
    expect(find.text('Great work — keep protein high and sleep 8h.'), findsWidgets);
    expect(tester.takeException(), isNull);
  });

}

void _reportTests() {
  testWidgets('"View full report" opens every reading with since-last / since-day-1 and InBody', (tester) async {
    await _pump(
      tester,
      ProgressJourneyView(
        journey: _journey(),
        isArabic: false,
        viewer: const MemberViewer(coachId: 'coach-1', coachName: 'Ahmed'),
        now: _now,
      ),
    );

    await tester.tap(find.text('View full report'));
    await tester.pumpAndSettle();

    expect(find.text('Check-in report'), findsOneWidget);
    expect(find.textContaining('Measured by Coach Ahmed'), findsOneWidget);
    expect(find.text('Since last'), findsOneWidget);
    expect(find.text('Since day 1'), findsOneWidget);
    expect(find.text('Body composition (InBody)'), findsOneWidget);
    expect(find.text('34.2 kg · 37.6%'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('tapping a check-in in the timeline opens its report (day one lists its readings)', (tester) async {
    await _pump(
      tester,
      ProgressJourneyView(journey: _journey(), isArabic: false, viewer: const MemberViewer(), now: _now),
    );

    await tester.tap(find.text('10 Oct 2026'));
    await tester.pumpAndSettle();

    expect(find.text('Check-in report'), findsOneWidget);
    expect(find.text('Day one readings'), findsOneWidget);
  });

}

void _coachTests() {
  testWidgets('coach view in Arabic on a small phone: own entries are "You", report opens, no overflow',
      (tester) async {
    final old = journeyOf([
      entry('2026-09-01', {ProgressMetric.weight: 101.5, ProgressMetric.waist: 110.5, ProgressMetric.leftArm: 33.5}),
      _withNote(entry('2026-10-20', {ProgressMetric.weight: 97.5, ProgressMetric.waist: 104, ProgressMetric.leftArm: 34}),
          'ممتاز، استمر على نفس النظام الغذائي وزوّد المياه'),
    ], goal: ProgressGoalType.loseFat, target: 90);
    await _pump(
      tester,
      ProgressJourneyView(journey: old, isArabic: true, viewer: const CoachViewer(coachId: 'coach-1'), now: _now),
      arabic: true,
      width: 320,
    );

    expect(find.text('آخر قياس'), findsOneWidget);
    expect(find.textContaining('أنت ·'), findsOneWidget);
    expect(find.text('جديد'), findsNothing, reason: '24 days ago');
    expect(tester.takeException(), isNull);

    await tester.tap(find.text('عرض التقرير كاملًا'));
    await tester.pumpAndSettle();
    expect(find.text('تقرير القياس'), findsOneWidget);
    expect(find.textContaining('القياس بواسطة أنت'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
