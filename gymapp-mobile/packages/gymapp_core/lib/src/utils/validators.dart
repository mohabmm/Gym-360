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

  /// Validate email address.
  static String? email(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar'
          ? 'البريد الإلكتروني مطلوب'
          : 'Email is required';
    }
    if (!_emailRegex.hasMatch(value.trim())) {
      return locale == 'ar'
          ? 'البريد الإلكتروني غير صالح'
          : 'Invalid email address';
    }
    return null;
  }

  /// Validate password (minimum 8 characters, at least 1 letter + 1 number).
  static String? password(String? value, {String locale = 'ar'}) {
    if (value == null || value.isEmpty) {
      return locale == 'ar' ? 'كلمة المرور مطلوبة' : 'Password is required';
    }
    if (value.length < 8) {
      return locale == 'ar'
          ? 'كلمة المرور يجب أن تكون 8 أحرف على الأقل'
          : 'Password must be at least 8 characters';
    }
    if (!value.contains(RegExp(r'[a-zA-Z]'))) {
      return locale == 'ar'
          ? 'كلمة المرور يجب أن تحتوي على حرف واحد على الأقل'
          : 'Password must contain at least one letter';
    }
    if (!value.contains(RegExp(r'[0-9]'))) {
      return locale == 'ar'
          ? 'كلمة المرور يجب أن تحتوي على رقم واحد على الأقل'
          : 'Password must contain at least one number';
    }
    return null;
  }

  /// Validate password confirmation.
  static String? confirmPassword(
    String? value,
    String password, {
    String locale = 'ar',
  }) {
    if (value == null || value.isEmpty) {
      return locale == 'ar'
          ? 'تأكيد كلمة المرور مطلوب'
          : 'Password confirmation is required';
    }
    if (value != password) {
      return locale == 'ar'
          ? 'كلمات المرور غير متطابقة'
          : 'Passwords do not match';
    }
    return null;
  }

  /// Validate required field.
  static String? required(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar' ? 'هذا الحقل مطلوب' : 'This field is required';
    }
    return null;
  }

  /// Validate name (2-50 characters).
  static String? name(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar' ? 'الاسم مطلوب' : 'Name is required';
    }
    if (value.trim().length < 2) {
      return locale == 'ar'
          ? 'الاسم يجب أن يكون حرفين على الأقل'
          : 'Name must be at least 2 characters';
    }
    if (value.trim().length > 50) {
      return locale == 'ar'
          ? 'الاسم يجب أن لا يتجاوز 50 حرف'
          : 'Name must not exceed 50 characters';
    }
    return null;
  }

  /// Validate phone number (Egyptian format).
  static String? phone(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return null; // Phone is optional
    }
    if (!_phoneRegex.hasMatch(value.replaceAll(RegExp(r'[\s-]'), ''))) {
      return locale == 'ar'
          ? 'رقم الهاتف غير صالح'
          : 'Invalid phone number';
    }
    return null;
  }

  /// Validate gym code (6 alphanumeric characters).
  static String? gymCode(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar' ? 'كود الجيم مطلوب' : 'Gym code is required';
    }
    if (!_gymCodeRegex.hasMatch(value.trim().toUpperCase())) {
      return locale == 'ar'
          ? 'كود الجيم يجب أن يكون 6 أحرف أو أرقام'
          : 'Gym code must be 6 alphanumeric characters';
    }
    return null;
  }

  /// Validate OTP (6 digits).
  static String? otp(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar' ? 'رمز التحقق مطلوب' : 'OTP is required';
    }
    if (value.trim().length != 6 || !RegExp(r'^[0-9]{6}$').hasMatch(value)) {
      return locale == 'ar'
          ? 'رمز التحقق يجب أن يكون 6 أرقام'
          : 'OTP must be 6 digits';
    }
    return null;
  }

  /// Validate positive number.
  static String? positiveNumber(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar' ? 'هذا الحقل مطلوب' : 'This field is required';
    }
    final number = double.tryParse(value);
    if (number == null || number <= 0) {
      return locale == 'ar'
          ? 'يرجى إدخال رقم صالح'
          : 'Please enter a valid number';
    }
    return null;
  }

  /// Validate weight (10-500 kg).
  static String? weight(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) return null;
    final number = double.tryParse(value);
    if (number == null || number < 10 || number > 500) {
      return locale == 'ar'
          ? 'الوزن يجب أن يكون بين 10 و 500 كجم'
          : 'Weight must be between 10 and 500 kg';
    }
    return null;
  }

  /// Validate height (50-300 cm).
  static String? height(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) return null;
    final number = double.tryParse(value);
    if (number == null || number < 50 || number > 300) {
      return locale == 'ar'
          ? 'الطول يجب أن يكون بين 50 و 300 سم'
          : 'Height must be between 50 and 300 cm';
    }
    return null;
  }

  /// Validate rating (1-5).
  static String? rating(int? value, {String locale = 'ar'}) {
    if (value == null || value < 1 || value > 5) {
      return locale == 'ar'
          ? 'التقييم يجب أن يكون بين 1 و 5'
          : 'Rating must be between 1 and 5';
    }
    return null;
  }

  /// Validate member number.
  static String? memberNumber(String? value, {String locale = 'ar'}) {
    if (value == null || value.trim().isEmpty) {
      return locale == 'ar' ? 'رقم العضو مطلوب' : 'Member number is required';
    }
    if (!RegExp(r'^[0-9]{4,8}$').hasMatch(value.trim())) {
      return locale == 'ar'
          ? 'رقم العضو غير صالح'
          : 'Invalid member number';
    }
    return null;
  }
}
