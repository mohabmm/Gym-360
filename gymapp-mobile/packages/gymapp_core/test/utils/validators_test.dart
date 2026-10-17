import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

typedef _Validate<T> = String? Function(T? value);

/// Asserts that [validate] rejects every value in [invalid] and accepts
/// every value in [valid].
void _expectCases<T>(
  _Validate<T> validate, {
  List<T?> invalid = const [],
  List<T?> valid = const [],
}) {
  for (final value in invalid) {
    expect(validate(value), isNotNull, reason: 'expected "$value" rejected');
  }
  for (final value in valid) {
    expect(validate(value), isNull, reason: 'expected "$value" accepted');
  }
}

void main() {
  group('Validators.email', () {
    test('returns error when null or empty', () {
      _expectCases<String>(Validators.email, invalid: [null, '', '   ']);
    });

    test('accepts common valid addresses', () {
      _expectCases<String>(
        Validators.email,
        valid: ['user@example.com', 'u.ser+tag@sub.example.co', 'A_B-C@mail.eg'],
      );
    });

    test('rejects malformed addresses', () {
      _expectCases<String>(
        Validators.email,
        invalid: ['user', 'user@', '@host.com', 'user@host', 'user@host.c'],
      );
    });

    test('localizes error messages', () {
      expect(Validators.email(null, locale: 'ar'), contains('البريد'));
      expect(Validators.email(null, locale: 'en'), contains('Email'));
    });
  });

  group('Validators.password', () {
    test('requires at least 8 chars with letter and number', () {
      _expectCases<String>(
        Validators.password,
        // too short, no number, no letter
        invalid: [null, '', 'abc123', 'abcdefgh', '12345678'],
        valid: ['abcd1234', 'Passw0rd'],
      );
    });
  });

  group('Validators.confirmPassword', () {
    test('must match', () {
      _expectCases<String>(
        (v) => Validators.confirmPassword(v, 'abc123'),
        invalid: [null, '', 'different'],
      );
      _expectCases<String>(
        (v) => Validators.confirmPassword(v, 'same'),
        valid: ['same'],
      );
    });
  });

  group('Validators.required', () {
    test('rejects null, empty and whitespace', () {
      _expectCases<String>(
        Validators.required,
        invalid: [null, '', '   '],
        valid: ['x'],
      );
    });
  });

  group('Validators.name', () {
    test('enforces 2-50 char length', () {
      _expectCases<String>(
        Validators.name,
        invalid: [null, 'a', 'a' * 51],
        valid: ['Ahmed', 'محمد'],
      );
    });
  });

  group('Validators.phone', () {
    test('is optional (null / empty returns null)', () {
      _expectCases<String>(Validators.phone, valid: [null, '']);
    });

    test('accepts Egyptian formats', () {
      _expectCases<String>(
        Validators.phone,
        valid: ['01012345678', '+201012345678', '010 1234 5678', '010-1234-5678'],
      );
    });

    test('rejects invalid formats', () {
      _expectCases<String>(
        Validators.phone,
        invalid: ['12345', '+1234567890123'],
      );
    });
  });

  group('Validators.gymCode', () {
    test('requires exactly 6 alphanumeric chars', () {
      _expectCases<String>(
        Validators.gymCode,
        invalid: [null, '', 'ABC12', 'ABC1234', 'ABC-12'],
        valid: ['ABC123', 'abc123'], // upper-cased internally
      );
    });
  });

  group('Validators.otp', () {
    test('must be exactly 6 digits', () {
      _expectCases<String>(
        Validators.otp,
        invalid: [null, '12345', '1234567', 'abcdef'],
        valid: ['123456'],
      );
    });
  });

  group('Validators.positiveNumber', () {
    test('requires >0', () {
      _expectCases<String>(
        Validators.positiveNumber,
        invalid: [null, '', 'abc', '0', '-1'],
        valid: ['10', '0.5'],
      );
    });
  });

  group('Validators.weight / height', () {
    test('weight range 10-500kg', () {
      _expectCases<String>(
        Validators.weight,
        invalid: ['9', '501', 'abc'],
        valid: ['', '80'], // optional
      );
    });

    test('height range 50-300cm', () {
      _expectCases<String>(
        Validators.height,
        invalid: ['49', '301'],
        valid: ['', '175'],
      );
    });
  });

  group('Validators.rating', () {
    test('must be 1-5', () {
      _expectCases<int>(
        Validators.rating,
        invalid: [null, 0, 6],
        valid: [1, 5],
      );
    });
  });

  group('Validators.memberNumber', () {
    test('accepts 4-8 digits', () {
      _expectCases<String>(
        Validators.memberNumber,
        invalid: [null, '', '123', '123456789', '12ab'],
        valid: ['1234', '12345678'],
      );
    });
  });
}
