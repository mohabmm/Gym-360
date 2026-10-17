import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/auth/data/datasources/auth_remote_datasource.dart';

import 'helpers/recorded_request.dart';
import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers every screen backed by [AuthRemoteDataSource]:
/// Login, Register, OTP (send + verify), Profile (get + update),
/// Link Gym, Social Login stub, Forgot Password, Consent.
///
/// Each test asserts (a) the correct HTTP method + path, (b) the request body
/// the BE expects, (c) the parsed response model matches the canned JSON.
void main() {
  late TestApiClient t;
  late AuthRemoteDataSource auth;

  setUp(() {
    t = buildTestApiClient();
    auth = AuthRemoteDataSource(apiClient: t.apiClient);
  });

  /// Registers a 204 for POST [path], runs [call] and asserts the JSON body.
  Future<void> expectPostBody(
    String path,
    Future<void> Function() call,
    Map<String, dynamic> body,
  ) async {
    t.adapter.onPost(path, respond: (_) => t.adapter.noContent());
    await call();
    expect(t.onlyRequest.bodyAsMap, body);
  }

  group('Login screen', () {
    test('POST /identity/login sends credentials and parses tokens + user',
        () async {
      t.adapter.onPost(
        ApiEndpoints.login,
        respond: (_) => t.adapter.ok(Samples.authResponse()),
      );

      final result = await auth.login(
        const LoginRequest(email: 'member@test.local', password: 'p@ss1234'),
      );

      expect(
        t.onlyRequest,
        isA<RecordedRequest>()
            .having((r) => r.method, 'method', 'POST')
            .having((r) => r.path, 'path', '/api/v1/identity/login')
            .having((r) => r.bodyAsMap, 'body', {
              'email': 'member@test.local',
              'password': 'p@ss1234',
            })
            // Login is a public endpoint — AuthInterceptor must NOT attach the token.
            .having((r) => r.header('authorization'), 'authorization', isNull),
      );
      expect(result.accessToken, 'new-access-token');
      expect(result.user.id, 'user-1');
    });

    test('401 response surfaces as UnauthorizedException', () async {
      t.adapter.onPost(
        ApiEndpoints.login,
        respond: (_) => t.adapter.error(
          401,
          code: 'INVALID_CREDENTIALS',
          message: 'Wrong email or password',
        ),
      );

      await expectLater(
        auth.login(const LoginRequest(email: 'a@b.c', password: 'x')),
        throwsA(isA<UnauthorizedException>()),
      );
    });
  });

  group('Register screen', () {
    test('POST /identity/register sends full signup payload', () async {
      t.adapter.onPost(
        ApiEndpoints.register,
        respond: (_) => t.adapter.ok(Samples.authResponse(), meta: null),
      );

      await auth.register(
        const RegisterRequest(
          email: 'new@test.local',
          password: 'Strong#123',
          firstName: 'Mona',
          lastName: 'Ali',
          phone: '+201000000000',
          role: 'Member',
          dataProcessingConsent: true,
          marketingConsent: false,
        ),
      );

      expect(
        t.onlyRequest.bodyAsMap,
        allOf(
          containsPair('email', 'new@test.local'),
          containsPair('role', 'Member'),
          containsPair('dataProcessingConsent', true),
          containsPair('marketingConsent', false),
        ),
      );
    });

    test('400 validation error maps to ValidationException with field errors',
        () async {
      t.adapter.onPost(
        ApiEndpoints.register,
        respond: (_) => t.adapter.rawResponse(400, {
          'data': null,
          'message': 'Validation failed',
          'errors': [
            {
              'code': 'VALIDATION_ERROR',
              'message': 'Email is already taken',
              'meta': {'field': 'Email'},
            },
            {
              'code': 'VALIDATION_ERROR',
              'message': 'Password must be 8+ chars',
              'meta': {'field': 'Password'},
            },
          ],
        }),
      );

      try {
        await auth.register(
          const RegisterRequest(
            email: 'bad',
            password: 'x',
            firstName: 'a',
            lastName: 'b',
            phone: '1',
            role: 'Member',
            dataProcessingConsent: true,
          ),
        );
        fail('expected ValidationException');
      } on ValidationException catch (e) {
        expect(e.fieldErrors['Email'], 'Email is already taken');
        expect(e.fieldErrors['Password'], contains('8+'));
      }
    });
  });

  group('OTP screen', () {
    test('POST /identity/send-otp carries email + purpose', () async {
      await expectPostBody(
        ApiEndpoints.sendOtp,
        () => auth.sendOtp(const OtpRequest(email: 'x@y.z', purpose: 'signup')),
        {'email': 'x@y.z', 'purpose': 'signup'},
      );
    });

    test('POST /identity/verify-otp sends the code', () async {
      await expectPostBody(
        ApiEndpoints.verifyOtp,
        () => auth.verifyOtp(
          const VerifyOtpRequest(email: 'x@y.z', otp: '123456'),
        ),
        {'email': 'x@y.z', 'otp': '123456'},
      );
    });
  });

  group('Profile screen', () {
    test('GET /identity/profile returns parsed UserModel', () async {
      t.adapter.onGet(
        ApiEndpoints.profile,
        respond: (_) => t.adapter.ok(Samples.user()),
      );

      final user = await auth.getProfile();
      expect(
        user,
        isA<UserModel>()
            .having((u) => u.id, 'id', 'user-1')
            .having((u) => u.role, 'role', 'Member'),
      );
      // Authorization header injected by AuthInterceptor for protected route.
      expect(
        t.onlyRequest,
        isA<RecordedRequest>()
            .having((r) => r.header('authorization'), 'authorization',
                'Bearer test-jwt')
            .having((r) => r.header('x-gym-id'), 'x-gym-id', 'gym-1'),
      );
    });

    test('PUT /identity/profile only serializes non-null fields and refetches',
        () async {
      t.adapter.onPut(
        ApiEndpoints.profile,
        respond: (_) => t.adapter.noContent(),
      );
      t.adapter.onGet(
        ApiEndpoints.profile,
        respond: (_) => t.adapter.ok(Samples.user()),
      );

      final updated = await auth.updateProfile(
        userId: 'user-1',
        request: UserProfileUpdateRequest(
          firstName: 'Mona',
          dateOfBirth: DateTime.utc(1998, 2, 3),
          gender: 'Female',
        ),
      );

      expect(updated.firstName, 'Mona');
      expect(t.adapter.recorded.map((r) => r.method).toList(), ['PUT', 'GET']);
      final putBody = t.adapter.recorded.first.bodyAsMap!;
      expect(
        putBody,
        allOf(
          // Caller is derived from the JWT — userId must not be sent.
          isNot(contains('userId')),
          containsPair('firstName', 'Mona'),
          containsPair('dateOfBirth', '1998-02-03'),
          containsPair('gender', 'Female'),
          // Fields we did not pass must be absent, not null.
          isNot(contains('lastName')),
          isNot(contains('phone')),
        ),
      );
    });
  });

  group('Gym linking', () {
    test('POST /identity/link-gym sends gymCode when provided', () async {
      t.adapter.onPost(
        ApiEndpoints.linkGym,
        respond: (_) => t.adapter.noContent(),
      );
      await auth.linkGym(const LinkGymRequest(gymCode: 'TAMEENI-1'));
      // Freezed's toJson emits null for optional fields — assert on the
      // populated key only so we don't pin serializer behaviour.
      expect(t.onlyRequest.bodyAsMap!['gymCode'], 'TAMEENI-1');
      expect(t.onlyRequest.bodyAsMap!['qrData'], isNull);
    });
  });

  group('Forgot password', () {
    test('POST /identity/forgot-password sends email', () async {
      await expectPostBody(
        ApiEndpoints.forgotPassword,
        () => auth.forgotPassword(
          const ForgotPasswordRequest(email: 'user@test.local'),
        ),
        {'email': 'user@test.local'},
      );
    });
  });

  group('Social login stub', () {
    test('POST /identity/social-login returns 404 until BE ships', () async {
      t.adapter.onPost(
        ApiEndpoints.socialLogin,
        respond: (_) => t.adapter.error(404, code: 'NOT_FOUND'),
      );
      await expectLater(
        auth.socialLogin(
          const SocialLoginRequest(provider: 'google', token: 'abc'),
        ),
        throwsA(isA<NotFoundException>()),
      );
    });
  });

  group('PDPL consent', () {
    test('POST /identity/consent serializes consentType, isGranted, text',
        () async {
      await expectPostBody(
        ApiEndpoints.consent,
        () => auth.recordConsent(
          consentType: 'Marketing',
          isGranted: true,
          consentText: 'v1',
        ),
        {'consentType': 'Marketing', 'isGranted': true, 'consentText': 'v1'},
      );
    });
  });
}

