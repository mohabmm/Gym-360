import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'journey_fixtures.dart';

void main() {
  group('normalizeNumber', () {
    test('Arabic-Indic digits and Arabic/comma decimal separators', () {
      expect(normalizeNumber('٨٢٫٥'), '82.5');
      expect(normalizeNumber(' 82,5 '), '82.5');
      expect(normalizeNumber('90'), '90');
    });
  });

  group('MeasurementInputFormatter', () {
    const f = MeasurementInputFormatter();
    TextEditingValue type(String old, String next) =>
        f.formatEditUpdate(TextEditingValue(text: old), TextEditingValue(text: next));

    test('allows up to 3 digits and 1 decimal', () {
      expect(type('82', '82.').text, '82.');
      expect(type('82.', '82.5').text, '82.5');
      expect(type('82.5', '82.55').text, '82.5');
      expect(type('100', '1000').text, '100');
      expect(type('', 'abc').text, '');
    });

    test('converts as typed', () => expect(type('', '٧٥').text, '75'));
  });

  group('checkMetric', () {
    test('empty is neither valid nor an error', () {
      final c = checkMetric(ProgressMetric.weight, '');
      expect(c.isValid, isFalse);
      expect(c.outOfRange, isFalse);
    });

    test('per-metric ranges', () {
      expect(checkMetric(ProgressMetric.weight, '19.9').outOfRange, isTrue);
      expect(checkMetric(ProgressMetric.weight, '350').isValid, isTrue);
      expect(checkMetric(ProgressMetric.bodyFat, '80').outOfRange, isTrue);
      expect(checkMetric(ProgressMetric.neck, '38').isValid, isTrue);
      expect(checkMetric(ProgressMetric.leftCalf, '7').outOfRange, isTrue);
    });

    test('more than 10% away from the last value is a warning, not an error', () {
      final c = checkMetric(ProgressMetric.waist, '70', previous: 90);
      expect(c.isValid, isTrue);
      expect(c.bigChange, isTrue);
      expect(checkMetric(ProgressMetric.waist, '85', previous: 90).bigChange, isFalse);
    });
  });

  group('history helpers', () {
    final journey = journeyOf([
      entry('2026-09-01', {ProgressMetric.weight: 95, ProgressMetric.waist: 100}),
      weighIn('2026-10-01', 92),
    ]);

    test('lastReadings takes each metric from the latest entry that has it', () {
      final last = lastReadings(journey);
      expect(last[ProgressMetric.weight]!.value, 92);
      expect(last[ProgressMetric.waist]!.value, 100);
      expect(last[ProgressMetric.waist]!.date, DateTime(2026, 9, 1));
      expect(last[ProgressMetric.neck], isNull);
    });

    test('hasEntryOn matches by calendar day', () {
      expect(hasEntryOn(journey, DateTime(2026, 10, 1, 18)), isTrue);
      expect(hasEntryOn(journey, DateTime(2026, 10, 2)), isFalse);
      expect(hasEntryOn(null, DateTime(2026, 10, 1)), isFalse);
    });

    test('apiDate pads to yyyy-MM-dd', () => expect(apiDate(DateTime(2026, 3, 7)), '2026-03-07'));
  });

  group('MeasurementFormController', () {
    test('reading() sends only valid values, tape in cm, and null tape when empty', () {
      final form = MeasurementFormController()..useJourney(journeyOf([weighIn('2026-10-01', 92)]));
      form.fields[ProgressMetric.weight]!.text = '91';
      form.fields[ProgressMetric.chest]!.text = '9';
      expect(form.hasErrors, isTrue);
      expect(form.canSubmit, isFalse);

      form.fields[ProgressMetric.chest]!.text = '';
      final reading = form.reading();
      expect(reading['weight'], 91);
      expect(reading['measurements'], isNull);
      expect(form.changes.single.delta.amount, -1);
      form.dispose();
    });
  });

  group('journeyWins', () {
    test('only goal-aligned changes, biggest relative first', () {
      final j = journeyOf([
        entry('2026-09-01', {ProgressMetric.weight: 100, ProgressMetric.waist: 100}),
        entry('2026-10-01', {ProgressMetric.weight: 98, ProgressMetric.waist: 90}),
      ], goal: ProgressGoalType.loseFat);
      expect(journeyWins(j).map((c) => c.metric), [ProgressMetric.waist, ProgressMetric.weight]);
    });

    test('changes away from the goal are not wins', () {
      final j = journeyOf([weighIn('2026-09-01', 90), weighIn('2026-10-01', 93)], goal: ProgressGoalType.loseFat);
      expect(journeyWins(j), isEmpty);
    });
  });
}
