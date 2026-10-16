import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/auth/presentation/screens/login_screen.dart';

void main() {
  group('localizeLoginError', () {
    const wrongCredentials = UnauthorizedException(
      message: 'The provided credentials are invalid.',
    );

    test('wrong password reads in Arabic on an Arabic UI', () {
      expect(
        localizeLoginError(wrongCredentials, true),
        'البريد الالكتروني او كلمة المرور غير صحيحة.',
      );
    });

    test('wrong password reads in English on an English UI', () {
      expect(
        localizeLoginError(wrongCredentials, false),
        'Incorrect email or password.',
      );
    });

    test('other API errors keep the server message', () {
      const locked = ApiException(
        message: 'Account locked',
        statusCode: 423,
        errorCode: 'ACCOUNT_LOCKED',
      );
      expect(localizeLoginError(locked, true), 'Account locked');
    });
  });
}
