import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/error_text.dart';

void main() {
  group('staffErrorText', () {
    test('translates known backend messages to Arabic', () {
      const error = ApiException(
        message: 'The member is already added to this gym.',
        statusCode: 400,
      );
      expect(
        staffErrorText(error, isArabic: true),
        'العضو مضاف بالفعل الى هذه الصالة.',
      );
      expect(staffErrorText(error, isArabic: false), error.message);
    });

    test('falls back to a status-based Arabic message for generic errors', () {
      const error = ForbiddenException(message: 'Something went wrong.');
      expect(
        staffErrorText(error, isArabic: true),
        'ليس لديك صلاحية لتنفيذ هذا الاجراء.',
      );
    });

    test('never leaks exception class prefixes', () {
      expect(
        staffErrorText(Exception('العضو غير موجود'), isArabic: true),
        'العضو غير موجود',
      );
      const api = ApiException(message: 'Unmapped detail', statusCode: 422);
      expect(staffErrorText(api, isArabic: true), 'Unmapped detail');
    });
  });
}
