import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainer/features/scheduling/presentation/screens/availability_parser.dart';

/// Locks in the Bug #5 fix: AvailabilityScreen must correctly hydrate from the
/// BE's actual response shape — `dayOfWeek` is a string like "monday" and
/// times arrive as "HH:mm:ss". Getting either wrong leaves the screen empty,
/// which was how the original data-loss bug manifested (Save on empty → wipe).
void main() {
  group('AvailabilityParser.parseDayOfWeek', () {
    test('maps lowercase enum names (BE default shape)', () {
      expect(AvailabilityParser.parseDayOfWeek('sunday'), 0);
      expect(AvailabilityParser.parseDayOfWeek('monday'), 1);
      expect(AvailabilityParser.parseDayOfWeek('saturday'), 6);
    });

    test('is case-insensitive for enum names', () {
      expect(AvailabilityParser.parseDayOfWeek('MONDAY'), 1);
      expect(AvailabilityParser.parseDayOfWeek('Wednesday'), 3);
    });

    test('accepts ints in the 0..6 range unchanged', () {
      for (var i = 0; i <= 6; i++) {
        expect(AvailabilityParser.parseDayOfWeek(i), i);
      }
    });

    test('accepts numeric strings', () {
      expect(AvailabilityParser.parseDayOfWeek('3'), 3);
    });

    test('rejects out-of-range ints', () {
      expect(AvailabilityParser.parseDayOfWeek(-1), isNull);
      expect(AvailabilityParser.parseDayOfWeek(7), isNull);
    });

    test('rejects unknown strings and unsupported types', () {
      expect(AvailabilityParser.parseDayOfWeek('not-a-day'), isNull);
      expect(AvailabilityParser.parseDayOfWeek(null), isNull);
      expect(AvailabilityParser.parseDayOfWeek(<String>[]), isNull);
    });
  });

  group('AvailabilityParser.parseTime', () {
    test('parses HH:mm:ss (BE default)', () {
      expect(AvailabilityParser.parseTime('10:00:00'),
          const TimeOfDay(hour: 10, minute: 0));
      expect(AvailabilityParser.parseTime('23:59:59'),
          const TimeOfDay(hour: 23, minute: 59));
    });

    test('parses HH:mm', () {
      expect(AvailabilityParser.parseTime('09:30'),
          const TimeOfDay(hour: 9, minute: 30));
    });

    test('rejects malformed input', () {
      expect(AvailabilityParser.parseTime('not-a-time'), isNull);
      expect(AvailabilityParser.parseTime('12'), isNull);
      expect(AvailabilityParser.parseTime(null), isNull);
      expect(AvailabilityParser.parseTime(1200), isNull);
    });

    test('rejects out-of-range hours and minutes', () {
      expect(AvailabilityParser.parseTime('24:00:00'), isNull);
      expect(AvailabilityParser.parseTime('10:60'), isNull);
    });
  });
}
