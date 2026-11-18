import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'journey_fixtures.dart';

final _now = DateTime(2026, 11, 13);

ProgressJourney _history() => journeyOf([
      entry('2026-10-01', {ProgressMetric.weight: 95, ProgressMetric.waist: 104}),
      weighIn('2026-11-01', 92),
    ], goal: ProgressGoalType.loseFat);

Future<void> _pump(WidgetTester tester, {required bool forCoach, bool arabic = false}) async {
  tester.view.physicalSize = const Size(1080, 8000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    locale: Locale(arabic ? 'ar' : 'en'),
    supportedLocales: const [Locale('ar'), Locale('en')],
    localizationsDelegates: GlobalMaterialLocalizations.delegates,
    home: MeasurementEntryPage(
      title: 'Record',
      isArabic: arabic,
      journey: _history(),
      forCoach: forCoach,
      now: _now,
      onSubmit: (_) async => null,
    ),
  ));
  await tester.pumpAndSettle();
}

String _text(WidgetTester tester, ProgressMetric m) =>
    tester.widget<TextField>(find.byKey(ValueKey('metric-${m.name}'))).controller!.text;

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  test('copyLast fills only empty fields; addNote adds each line once', () {
    final form = MeasurementFormController()..useJourney(_history());
    form.fields[ProgressMetric.weight]!.text = '91';

    expect(form.canCopyLast, isTrue);
    form.copyLast();
    expect(form.fields[ProgressMetric.weight]!.text, '91');
    expect(form.fields[ProgressMetric.waist]!.text, '104');
    expect(form.fields[ProgressMetric.chest]!.text, isEmpty);
    expect(form.canCopyLast, isFalse);

    form
      ..addNote('Drink more water')
      ..addNote('Drink more water')
      ..addNote('Sleep 7–8 hours');
    expect(form.notes.text, 'Drink more water\nSleep 7–8 hours');
    form.dispose();
  });

  testWidgets('"Same as last" copies the last readings in one tap', (tester) async {
    await _pump(tester, forCoach: false);

    await tester.tap(find.byKey(const ValueKey('copy-last')));
    await tester.pumpAndSettle();

    expect(_text(tester, ProgressMetric.weight), '92');
    expect(_text(tester, ProgressMetric.waist), '104');
    final tile = tester.widget<ListTile>(find.descendant(of: find.byKey(const ValueKey('copy-last')), matching: find.byType(ListTile)));
    expect(tile.enabled, isFalse, reason: 'nothing left to copy; the row stays so fields do not jump');
  });

  testWidgets('coach: note is addressed to the client and quick notes add a line', (tester) async {
    await _pump(tester, forCoach: true);

    expect(find.text('Note for your client (optional)'), findsOneWidget);
    final chip = find.widgetWithText(ActionChip, 'Drink more water');
    await tester.ensureVisible(chip);
    await tester.tap(chip);
    await tester.pump();
    expect(find.widgetWithText(TextField, 'Drink more water'), findsOneWidget);
  });

  testWidgets('member: no quick notes; Arabic coach form has no overflow', (tester) async {
    await _pump(tester, forCoach: false);
    expect(find.text('Drink more water'), findsNothing);

    await _pump(tester, forCoach: true, arabic: true);
    expect(find.text('زوّد شرب المياه'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('coach wording: client wins and "check-in due for this client"', (tester) async {
    final overdue = journeyOf([weighIn('2026-10-01', 95), weighIn('2026-10-20', 92)], goal: ProgressGoalType.loseFat);
    const strings = ProgressJourneyStrings(false);
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: Column(children: [
          NextCheckInCard(journey: overdue, strings: strings, now: _now, onLog: () {}, forCoach: true),
          ProgressWinsCard(journey: overdue, strings: strings, now: _now, forCoach: true),
        ]),
      ),
    ));

    expect(find.text('Check-in due for this client'), findsOneWidget);
    expect(find.text("Client's wins since day one"), findsOneWidget);
    expect(find.text('Your wins since day one'), findsNothing);
  });
}
