import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('Validators.email', () {
    test('returns error when null or empty', () {
      expect(Validators.email(null), isNotNull);
      expect(Validators.email(''), isNotNull);
      expect(Validators.email('   '), isNotNull);
    });

    test('accepts common valid addresses', () {
      expect(Validators.email('user@example.com'), isNull);
      expect(Validators.email('u.ser+tag@sub.example.co'), isNull);
      expect(Validators.email('A_B-C@mail.eg'), isNull);
    });

    test('rejects malformed addresses', () {
      expect(Validators.email('user'), isNotNull);
      expect(Validators.email('user@'), isNotNull);
      expect(Validators.email('@host.com'), isNotNull);
      expect(Validators.email('user@host'), isNotNull);
      expect(Validators.email('user@host.c'), isNotNull);
    });

    test('localizes error messages', () {
      expect(Validators.email(null, locale: 'ar'), contains('البريد'));
      expect(Validators.email(null, locale: 'en'), contains('Email'));
    });
  });

  group('Validators.password', () {
    test('requires at least 8 chars with letter and number', () {
      expect(Validators.password(null), isNotNull);
      expect(Validators.password(''), isNotNull);
      expect(Validators.password('abc123'), isNotNull); // too short
      expect(Validators.password('abcdefgh'), isNotNull); // no number
      expect(Validators.password('12345678'), isNotNull); // no letter
      expect(Validators.password('abcd1234'), isNull);
      expect(Validators.password('Passw0rd'), isNull);
    });
  });

  group('Validators.confirmPassword', () {
    test('must match', () {
      expect(Validators.confirmPassword(null, 'abc123'), isNotNull);
      expect(Validators.confirmPassword('', 'abc123'), isNotNull);
      expect(Validators.confirmPassword('different', 'abc123'), isNotNull);
      expect(Validators.confirmPassword('same', 'same'), isNull);
    });
  });

  group('Validators.required', () {
    test('rejects null, empty and whitespace', () {
      expect(Validators.required(null), isNotNull);
      expect(Validators.required(''), isNotNull);
      expect(Validators.required('   '), isNotNull);
      expect(Validators.required('x'), isNull);
    });
  });

  group('Validators.name', () {
    test('enforces 2-50 char length', () {
      expect(Validators.name(null), isNotNull);
      expect(Validators.name('a'), isNotNull);
      expect(Validators.name('a' * 51), isNotNull);
      expect(Validators.name('Ahmed'), isNull);
      expect(Validators.name('محمد'), isNull);
    });
  });

  group('Validators.phone', () {
    test('is optional (null / empty returns null)', () {
      expect(Validators.phone(null), isNull);
      expect(Validators.phone(''), isNull);
    });

    test('accepts Egyptian formats', () {
      expect(Validators.phone('01012345678'), isNull);
      expect(Validators.phone('+201012345678'), isNull);
      expect(Validators.phone('010 1234 5678'), isNull);
      expect(Validators.phone('010-1234-5678'), isNull);
    });

    test('rejects invalid formats', () {
      expect(Validators.phone('12345'), isNotNull);
      expect(Validators.phone('+1234567890123'), isNotNull);
    });
  });

  group('Validators.gymCode', () {
    test('requires exactly 6 alphanumeric chars', () {
      expect(Validators.gymCode(null), isNotNull);
      expect(Validators.gymCode(''), isNotNull);
      expect(Validators.gymCode('ABC12'), isNotNull);
      expect(Validators.gymCode('ABC1234'), isNotNull);
      expect(Validators.gymCode('ABC-12'), isNotNull);
      expect(Validators.gymCode('ABC123'), isNull);
      expect(Validators.gymCode('abc123'), isNull); // upper-cased internally
    });
  });

  group('Validators.otp', () {
    test('must be exactly 6 digits', () {
      expect(Validators.otp(null), isNotNull);
      expect(Validators.otp('12345'), isNotNull);
      expect(Validators.otp('1234567'), isNotNull);
      expect(Validators.otp('abcdef'), isNotNull);
      expect(Validators.otp('123456'), isNull);
    });
  });

  group('Validators.positiveNumber', () {
    test('requires >0', () {
      expect(Validators.positiveNumber(null), isNotNull);
      expect(Validators.positiveNumber(''), isNotNull);
      expect(Validators.positiveNumber('abc'), isNotNull);
      expect(Validators.positiveNumber('0'), isNotNull);
      expect(Validators.positiveNumber('-1'), isNotNull);
      expect(Validators.positiveNumber('10'), isNull);
      expect(Validators.positiveNumber('0.5'), isNull);
    });
  });

  group('Validators.weight / height', () {
    test('weight range 10-500kg', () {
      expect(Validators.weight(''), isNull); // optional
      expect(Validators.weight('9'), isNotNull);
      expect(Validators.weight('501'), isNotNull);
      expect(Validators.weight('abc'), isNotNull);
      expect(Validators.weight('80'), isNull);
    });

    test('height range 50-300cm', () {
      expect(Validators.height(''), isNull);
      expect(Validators.height('49'), isNotNull);
      expect(Validators.height('301'), isNotNull);
      expect(Validators.height('175'), isNull);
    });
  });

  group('Validators.rating', () {
    test('must be 1-5', () {
      expect(Validators.rating(null), isNotNull);
      expect(Validators.rating(0), isNotNull);
      expect(Validators.rating(6), isNotNull);
      expect(Validators.rating(1), isNull);
      expect(Validators.rating(5), isNull);
    });
  });

  group('Validators.memberNumber', () {
    test('accepts 4-8 digits', () {
      expect(Validators.memberNumber(null), isNotNull);
      expect(Validators.memberNumber(''), isNotNull);
      expect(Validators.memberNumber('123'), isNotNull);
      expect(Validators.memberNumber('123456789'), isNotNull);
      expect(Validators.memberNumber('12ab'), isNotNull);
      expect(Validators.memberNumber('1234'), isNull);
      expect(Validators.memberNumber('12345678'), isNull);
    });
  });
}
