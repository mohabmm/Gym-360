import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  group('DateFormatter.formatShort', () {
    test('formats as dd/MM/yyyy', () {
      expect(DateFormatter.formatShort(DateTime(2024, 1, 12)), '12/01/2024');
    });
  });

  group('DateFormatter.formatTime24', () {
    test('formats as HH:mm', () {
      expect(DateFormatter.formatTime24(DateTime(2024, 1, 12, 9, 5)), '09:05');
      expect(DateFormatter.formatTime24(DateTime(2024, 1, 12, 23, 59)),
          '23:59');
    });
  });

  group('DateFormatter.formatRelative', () {
    test('shows "just now" within 60s', () {
      final now = DateTime.now().subtract(const Duration(seconds: 10));
      expect(DateFormatter.formatRelative(now, locale: 'en'), 'Just now');
      expect(DateFormatter.formatRelative(now, locale: 'ar'), 'الآن');
    });

    test('shows minutes ago', () {
      final d = DateTime.now().subtract(const Duration(minutes: 5));
      expect(DateFormatter.formatRelative(d, locale: 'en'), '5m ago');
    });

    test('shows hours ago', () {
      final d = DateTime.now().subtract(const Duration(hours: 3));
      expect(DateFormatter.formatRelative(d, locale: 'en'), '3h ago');
    });

    test('shows days ago', () {
      final d = DateTime.now().subtract(const Duration(days: 2));
      expect(DateFormatter.formatRelative(d, locale: 'en'), '2d ago');
    });

    test('falls back to short date after 7 days', () {
      final d = DateTime.now().subtract(const Duration(days: 30));
      final result = DateFormatter.formatRelative(d, locale: 'en');
      expect(result, DateFormatter.formatShort(d));
    });
  });

  group('DateFormatter.isToday / isTomorrow', () {
    test('today', () {
      expect(DateFormatter.isToday(DateTime.now()), isTrue);
      expect(DateFormatter.isToday(DateTime(2000, 1, 1)), isFalse);
    });

    test('tomorrow', () {
      final tomorrow = DateTime.now().add(const Duration(days: 1));
      expect(DateFormatter.isTomorrow(tomorrow), isTrue);
      expect(DateFormatter.isTomorrow(DateTime.now()), isFalse);
    });
  });

  group('DateFormatter.formatDuration', () {
    test('< 60 min returns minutes', () {
      expect(DateFormatter.formatDuration(45, locale: 'en'), '45m');
      expect(DateFormatter.formatDuration(45, locale: 'ar'), '45 د');
    });

    test('exact hours', () {
      expect(DateFormatter.formatDuration(120, locale: 'en'), '2h');
      expect(DateFormatter.formatDuration(120, locale: 'ar'), '2 س');
    });

    test('hours with minutes', () {
      expect(DateFormatter.formatDuration(90, locale: 'en'), '1h 30m');
      expect(DateFormatter.formatDuration(90, locale: 'ar'), '1 س 30 د');
    });
  });

  group('DateFormatter.getGreeting', () {
    test('returns one of the expected greetings', () {
      final ar = DateFormatter.getGreeting(locale: 'ar');
      final en = DateFormatter.getGreeting(locale: 'en');
      expect(['صباح الخير', 'مساء الخير'], contains(ar));
      expect(['Good morning', 'Good afternoon', 'Good evening'], contains(en));
    });
  });
}
