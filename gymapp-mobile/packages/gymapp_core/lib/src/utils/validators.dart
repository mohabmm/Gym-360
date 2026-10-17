/// A validation message available in Arabic and English.
class LocalizedMessage {
  const LocalizedMessage(this.ar, this.en);

  final String ar;
  final String en;

  /// Arabic for `'ar'`, English for any other locale.
  String resolve(String locale) => locale == 'ar' ? ar : en;
}

/// A single validation rule: when [fails] matches, [message] is reported.
class ValidationRule<T> {
  const ValidationRule(this.fails, this.message);

  final bool Function(T value) fails;
  final LocalizedMessage message;
}

/// An inclusive numeric range.
class NumRange {
  const NumRange(this.min, this.max);

  final num min;
  final num max;

  bool excludes(num? number) => number == null || number < min || number > max;
}

/// How an empty text value is detected before the rules run.
enum BlankCheck {
  /// Empty after trimming whitespace.
  trimmed,

  /// Literally empty (whitespace counts as a value).
  untrimmed;

  bool isBlank(String value) =>
      (this == BlankCheck.trimmed ? value.trim() : value).isEmpty;
}

/// Runs an ordered list of [ValidationRule]s and returns the first failure.
///
/// A `null` (or [isBlank]) value either reports [requiredMessage] or, when
/// that is `null` (optional field), passes without running the rules.
class FieldValidator<T> {
  const FieldValidator(this.rules, {this.requiredMessage, this.isBlank});

  final List<ValidationRule<T>> rules;
  final LocalizedMessage? requiredMessage;
  final bool Function(T value)? isBlank;

  String? call(T? value, {String locale = 'ar'}) {
    final failed = value == null || (isBlank?.call(value) ?? false)
        ? requiredMessage
        : _firstFailure(value);
    return failed?.resolve(locale);
  }

  LocalizedMessage? _firstFailure(T value) {
    for (final rule in rules) {
      if (rule.fails(value)) return rule.message;
    }
    return null;
  }
}

/// Builds a [FieldValidator] for text fields with the shared blank check.
FieldValidator<String> _text(
  List<ValidationRule<String>> rules, {
  LocalizedMessage? required,
  BlankCheck blank = BlankCheck.trimmed,
}) =>
    FieldValidator<String>(
      rules,
      requiredMessage: required,
      isBlank: blank.isBlank,
    );

/// Fails when the (normalised) value does not match [pattern].
ValidationRule<String> _pattern(
  RegExp pattern,
  LocalizedMessage message, {
  String Function(String value)? normalize,
}) =>
    ValidationRule(
      (v) => !pattern.hasMatch(normalize == null ? v.trim() : normalize(v)),
      message,
    );

/// Fails when the parsed number falls outside [range].
ValidationRule<String> _numberIn(NumRange range, LocalizedMessage message) =>
    ValidationRule((v) => range.excludes(double.tryParse(v)), message);

/// Form field validators supporting Arabic and English error messages.
class Validators {
  Validators._();

  static final RegExp _emailRegex = RegExp(
    r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$',
  );

  static final RegExp _phoneRegex = RegExp(
    r'^(\+?2)?0[0-9]{10}$', // Egyptian phone numbers
  );

  static final RegExp _gymCodeRegex = RegExp(
    r'^[A-Z0-9]{6}$',
  );

  static final RegExp _phoneSeparators = RegExp(r'[\s-]');
  static final RegExp _letter = RegExp(r'[a-zA-Z]');
  static final RegExp _digit = RegExp(r'[0-9]');
  static final RegExp _otpRegex = RegExp(r'^[0-9]{6}$');
  static final RegExp _memberNumberRegex = RegExp(r'^[0-9]{4,8}$');

  static const _fieldRequired =
      LocalizedMessage('هذا الحقل مطلوب', 'This field is required');

  /// Validate email address.
  static final email = _text(
    [
      _pattern(
        _emailRegex,
        const LocalizedMessage(
          'البريد الإلكتروني غير صالح',
          'Invalid email address',
        ),
      ),
    ],
    required: const LocalizedMessage(
      'البريد الإلكتروني مطلوب',
      'Email is required',
    ),
  );

  /// Validate password (minimum 8 characters, at least 1 letter + 1 number).
  static final password = _text(
    [
      ValidationRule(
        (v) => v.length < 8,
        const LocalizedMessage(
          'كلمة المرور يجب أن تكون 8 أحرف على الأقل',
          'Password must be at least 8 characters',
        ),
      ),
      ValidationRule(
        (v) => !v.contains(_letter),
        const LocalizedMessage(
          'كلمة المرور يجب أن تحتوي على حرف واحد على الأقل',
          'Password must contain at least one letter',
        ),
      ),
      ValidationRule(
        (v) => !v.contains(_digit),
        const LocalizedMessage(
          'كلمة المرور يجب أن تحتوي على رقم واحد على الأقل',
          'Password must contain at least one number',
        ),
      ),
    ],
    required:
        const LocalizedMessage('كلمة المرور مطلوبة', 'Password is required'),
    blank: BlankCheck.untrimmed,
  );

  /// Validate password confirmation.
  static String? confirmPassword(
    String? value,
    String password, {
    String locale = 'ar',
  }) {
    return _text(
      [
        ValidationRule(
          (v) => v != password,
          const LocalizedMessage(
            'كلمات المرور غير متطابقة',
            'Passwords do not match',
          ),
        ),
      ],
      required: const LocalizedMessage(
        'تأكيد كلمة المرور مطلوب',
        'Password confirmation is required',
      ),
      blank: BlankCheck.untrimmed,
    )(value, locale: locale);
  }

  /// Validate required field.
  static final required = _text(const [], required: _fieldRequired);

  /// Validate name (2-50 characters).
  static final name = _text(
    [
      ValidationRule(
        (v) => v.trim().length < 2,
        const LocalizedMessage(
          'الاسم يجب أن يكون حرفين على الأقل',
          'Name must be at least 2 characters',
        ),
      ),
      ValidationRule(
        (v) => v.trim().length > 50,
        const LocalizedMessage(
          'الاسم يجب أن لا يتجاوز 50 حرف',
          'Name must not exceed 50 characters',
        ),
      ),
    ],
    required: const LocalizedMessage('الاسم مطلوب', 'Name is required'),
  );

  /// Validate phone number (Egyptian format). Phone is optional.
  static final phone = _text([
    _pattern(
      _phoneRegex,
      const LocalizedMessage('رقم الهاتف غير صالح', 'Invalid phone number'),
      normalize: (v) => v.replaceAll(_phoneSeparators, ''),
    ),
  ]);

  /// Validate gym code (6 alphanumeric characters).
  static final gymCode = _text(
    [
      _pattern(
        _gymCodeRegex,
        const LocalizedMessage(
          'كود الجيم يجب أن يكون 6 أحرف أو أرقام',
          'Gym code must be 6 alphanumeric characters',
        ),
        normalize: (v) => v.trim().toUpperCase(),
      ),
    ],
    required: const LocalizedMessage('كود الجيم مطلوب', 'Gym code is required'),
  );

  /// Validate OTP (6 digits).
  static final otp = _text(
    [
      ValidationRule(
        (v) => v.trim().length != 6 || !_otpRegex.hasMatch(v),
        const LocalizedMessage(
          'رمز التحقق يجب أن يكون 6 أرقام',
          'OTP must be 6 digits',
        ),
      ),
    ],
    required: const LocalizedMessage('رمز التحقق مطلوب', 'OTP is required'),
  );

  /// Validate positive number.
  static final positiveNumber = _text(
    [
      ValidationRule(
        (v) => (double.tryParse(v) ?? 0) <= 0,
        const LocalizedMessage(
          'يرجى إدخال رقم صالح',
          'Please enter a valid number',
        ),
      ),
    ],
    required: _fieldRequired,
  );

  /// Validate weight (10-500 kg). Weight is optional.
  static final weight = _text([
    _numberIn(
      const NumRange(10, 500),
      const LocalizedMessage(
        'الوزن يجب أن يكون بين 10 و 500 كجم',
        'Weight must be between 10 and 500 kg',
      ),
    ),
  ]);

  /// Validate height (50-300 cm). Height is optional.
  static final height = _text([
    _numberIn(
      const NumRange(50, 300),
      const LocalizedMessage(
        'الطول يجب أن يكون بين 50 و 300 سم',
        'Height must be between 50 and 300 cm',
      ),
    ),
  ]);

  static const _ratingOutOfRange = LocalizedMessage(
    'التقييم يجب أن يكون بين 1 و 5',
    'Rating must be between 1 and 5',
  );

  /// Validate rating (1-5).
  static final rating = FieldValidator<int>(
    [ValidationRule(const NumRange(1, 5).excludes, _ratingOutOfRange)],
    requiredMessage: _ratingOutOfRange,
  );

  /// Validate member number.
  static final memberNumber = _text(
    [
      _pattern(
        _memberNumberRegex,
        const LocalizedMessage('رقم العضو غير صالح', 'Invalid member number'),
      ),
    ],
    required: const LocalizedMessage(
      'رقم العضو مطلوب',
      'Member number is required',
    ),
  );
}
