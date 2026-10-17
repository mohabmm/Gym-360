import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/date_symbol_data_local.dart';

const _ar = AppLanguage.arabic;
const _en = AppLanguage.english;

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  group('NumberFormatter.formatCurrency', () {
    test('arabic uses ج.م suffix', () {
      final result =
          NumberFormatter.formatCurrency(const Egp(1500), language: _ar);
      expect(result, contains('ج.م'));
    });

    test('english uses currency prefix', () {
      final result =
          NumberFormatter.formatCurrency(const Egp(1500), language: _en);
      expect(result, startsWith('EGP'));
    });
  });

  group('NumberFormatter.formatPercentage', () {
    test('appends percent sign with decimals', () {
      expect(NumberFormatter.formatPercentage(const Percent(42.5)), '42.5%');
      expect(
        NumberFormatter.formatPercentage(const Percent(100), decimals: 0),
        '100%',
      );
    });
  });

  group('NumberFormatter.formatWeight / formatHeight', () {
    test('formats kg with one decimal', () {
      expect(NumberFormatter.formatWeight(80.2), '80.2 kg');
    });

    test('formats height default no decimals', () {
      expect(NumberFormatter.formatHeight(const Centimeters(175.4)), '175 cm');
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
    String water(int ml, AppLanguage language) =>
        NumberFormatter.formatWater(Milliliters(ml), language: language);

    test('below 1000 uses ml/مل', () {
      expect(water(500, _ar), '500 مل');
      expect(water(500, _en), '500ml');
    });

    test('whole liters have no decimal', () {
      expect(water(2000, _en), '2L');
      expect(water(2000, _ar), '2 لتر');
    });

    test('partial liters keep one decimal', () {
      expect(water(1500, _en), '1.5L');
    });
  });

  group('NumberFormatter.formatRating', () {
    test('always one decimal', () {
      expect(NumberFormatter.formatRating(const StarRating(4.0)), '4.0');
      expect(NumberFormatter.formatRating(const StarRating(4.75)), '4.8');
    });
  });

  group('NumberFormatter.formatOrdinal', () {
    test('english ordinal suffixes', () {
      const expected = {
        1: '1st',
        2: '2nd',
        3: '3rd',
        4: '4th',
        11: '11th',
        12: '12th',
        13: '13th',
        21: '21st',
        112: '112th',
      };
      expected.forEach((number, ordinal) {
        expect(NumberFormatter.formatOrdinal(number, language: _en), ordinal);
      });
    });

    test('arabic uses # prefix', () {
      expect(NumberFormatter.formatOrdinal(5, language: _ar), '#5');
    });
  });

  group('NumberFormatter.formatSessionCount', () {
    test('arabic uses جلسة label', () {
      final label = NumberFormatter.formatSessionCount(
        const SessionUsage(used: 3, total: 12),
        language: _ar,
      );
      expect(label, contains('3/12'));
      expect(label, contains('جلسة'));
    });
  });
}
