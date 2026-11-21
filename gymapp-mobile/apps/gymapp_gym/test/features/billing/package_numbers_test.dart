import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_gym/features/billing/presentation/package_numbers.dart';

void main() {
  test('sessions accept Arabic-Indic and Western digits', () {
    const cases = {'12': 12, '١٢': 12, ' ٢٤ ': 24, '': null, '12a': null, '١٢.٥': null};
    for (final MapEntry(key: text, value: expected) in cases.entries) {
      expect(PackageNumbers.sessions(text), expected, reason: '"$text"');
    }
  });

  test('price accepts Arabic digits, thousands separators and decimals', () {
    const cases = {
      '1800': 1800.0,
      '١٨٠٠': 1800.0,
      '1,800': 1800.0,
      '١٬٨٠٠': 1800.0,
      '1800.5': 1800.5,
      '١٨٠٠٫٥': 1800.5,
      '': null,
      'abc': null,
    };
    for (final MapEntry(key: text, value: expected) in cases.entries) {
      expect(PackageNumbers.price(text), expected, reason: '"$text"');
    }
  });
}
