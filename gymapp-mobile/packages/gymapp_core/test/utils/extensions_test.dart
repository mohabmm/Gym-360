import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('StringExtensions', () {
    test('capitalize', () {
      expect('hello'.capitalize(), 'Hello');
      expect(''.capitalize(), '');
      expect('A'.capitalize(), 'A');
    });

    test('toColor handles 6 and 7 length hex', () {
      expect('#FF0000'.toColor().value, const Color(0xFFFF0000).value);
      expect('00FF00'.toColor().value, const Color(0xFF00FF00).value);
    });

    test('isEmail', () {
      expect('user@example.com'.isEmail, isTrue);
      expect('not-an-email'.isEmail, isFalse);
    });

    test('isPhone - Egyptian', () {
      expect('01012345678'.isPhone, isTrue);
      expect('+201012345678'.isPhone, isTrue);
      expect('123'.isPhone, isFalse);
    });

    test('truncate', () {
      expect('hello world'.truncate(5), 'hello...');
      expect('short'.truncate(20), 'short');
    });

    test('initials', () {
      expect('Ahmed Ali'.initials, 'AA');
      expect('Mohamed'.initials, 'M');
      expect('First Middle Last'.initials, 'FL');
    });
  });

  group('DateTimeExtensions', () {
    test('isToday / isTomorrow / isYesterday', () {
      final now = DateTime.now();
      expect(now.isToday, isTrue);
      expect(now.add(const Duration(days: 1)).isTomorrow, isTrue);
      expect(now.subtract(const Duration(days: 1)).isYesterday, isTrue);
    });

    test('startOfDay / endOfDay', () {
      final date = DateTime(2024, 1, 12, 15, 30);
      expect(date.startOfDay, DateTime(2024, 1, 12));
      expect(date.endOfDay.hour, 23);
      expect(date.endOfDay.minute, 59);
    });

    test('startOfMonth', () {
      expect(DateTime(2024, 3, 25).startOfMonth, DateTime(2024, 3, 1));
    });

    test('timeString is zero-padded', () {
      expect(DateTime(2024, 1, 1, 9, 5).timeString, '09:05');
    });
  });

  group('NumExtensions', () {
    test('toPercentage', () {
      expect(12.5.toPercentage(), '12.5%');
      expect(50.toPercentage(decimals: 0), '50%');
    });

    test('toCurrency', () {
      expect(1500.toCurrency(locale: 'ar'), contains('ج.م'));
      expect(1500.toCurrency(locale: 'en'), startsWith('EGP'));
    });
  });

  group('ListExtensions', () {
    test('getOrNull', () {
      final list = [1, 2, 3];
      expect(list.getOrNull(0), 1);
      expect(list.getOrNull(5), isNull);
      expect(list.getOrNull(-1), isNull);
    });

    test('groupBy', () {
      final list = ['apple', 'ant', 'banana', 'berry'];
      final grouped = list.groupBy<String>((e) => e[0]);
      expect(grouped['a'], ['apple', 'ant']);
      expect(grouped['b'], ['banana', 'berry']);
    });
  });
}
