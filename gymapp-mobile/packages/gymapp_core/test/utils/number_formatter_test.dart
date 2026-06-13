import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/date_symbol_data_local.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  group('NumberFormatter.formatCurrency', () {
    test('arabic uses ج.م suffix', () {
      final result = NumberFormatter.formatCurrency(1500, locale: 'ar');
      expect(result, contains('ج.م'));
    });

    test('english uses currency prefix', () {
      final result = NumberFormatter.formatCurrency(1500, locale: 'en');
      expect(result, startsWith('EGP'));
    });
  });

  group('NumberFormatter.formatPercentage', () {
    test('appends percent sign with decimals', () {
      expect(NumberFormatter.formatPercentage(42.5), '42.5%');
      expect(NumberFormatter.formatPercentage(100, decimals: 0), '100%');
    });
  });

  group('NumberFormatter.formatWeight / formatHeight', () {
    test('formats kg with one decimal', () {
      expect(NumberFormatter.formatWeight(80.2), '80.2 kg');
    });

    test('formats height default no decimals', () {
      expect(NumberFormatter.formatHeight(175.4), '175 cm');
    });
  });

  group('NumberFormatter.formatWithSign', () {
    test('prefixes positive with +', () {
      expect(NumberFormatter.formatWithSign(3.2), '+3.2');
    });

    test('leaves negative numbers as-is', () {
      expect(NumberFormatter.formatWithSign(-1.5), '-1.5');
    });

    test('no + for zero', () {
      expect(NumberFormatter.formatWithSign(0), '0.0');
    });
  });

  group('NumberFormatter.formatWater', () {
    test('below 1000 uses ml/مل', () {
      expect(NumberFormatter.formatWater(500, locale: 'ar'), '500 مل');
      expect(NumberFormatter.formatWater(500, locale: 'en'), '500ml');
    });

    test('whole liters have no decimal', () {
      expect(NumberFormatter.formatWater(2000, locale: 'en'), '2L');
      expect(NumberFormatter.formatWater(2000, locale: 'ar'), '2 لتر');
    });

    test('partial liters keep one decimal', () {
      expect(NumberFormatter.formatWater(1500, locale: 'en'), '1.5L');
    });
  });

  group('NumberFormatter.formatRating', () {
    test('always one decimal', () {
      expect(NumberFormatter.formatRating(4.0), '4.0');
      expect(NumberFormatter.formatRating(4.75), '4.8');
    });
  });

  group('NumberFormatter.formatOrdinal', () {
    test('english ordinal suffixes', () {
      expect(NumberFormatter.formatOrdinal(1, locale: 'en'), '1st');
      expect(NumberFormatter.formatOrdinal(2, locale: 'en'), '2nd');
      expect(NumberFormatter.formatOrdinal(3, locale: 'en'), '3rd');
      expect(NumberFormatter.formatOrdinal(4, locale: 'en'), '4th');
      expect(NumberFormatter.formatOrdinal(11, locale: 'en'), '11th');
      expect(NumberFormatter.formatOrdinal(12, locale: 'en'), '12th');
      expect(NumberFormatter.formatOrdinal(13, locale: 'en'), '13th');
      expect(NumberFormatter.formatOrdinal(21, locale: 'en'), '21st');
      expect(NumberFormatter.formatOrdinal(112, locale: 'en'), '112th');
    });

    test('arabic uses # prefix', () {
      expect(NumberFormatter.formatOrdinal(5, locale: 'ar'), '#5');
    });
  });

  group('NumberFormatter.formatSessionCount', () {
    test('arabic uses جلسة label', () {
      expect(NumberFormatter.formatSessionCount(3, 12, locale: 'ar'),
          contains('3/12'));
      expect(NumberFormatter.formatSessionCount(3, 12, locale: 'ar'),
          contains('جلسة'));
    });
  });
}
