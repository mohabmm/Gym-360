import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers every auth-related screen the trainer app exposes:
/// LoginScreen, RegisterScreen, PendingApprovalScreen (profile refresh),
/// and the GET /profile call used by AuthNotifier._checkAuth.
///
/// Each test locks down the HTTP contract: method, path, query, headers,
/// request body, parsed response model.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient(
      // login / register run BEFORE the user is authenticated — start clean.
      accessToken: '',
      refreshToken: '',
      userId: '',
      gymId: '',
    );
  });

  group('LoginScreen — POST /identity/login', () {
    test('sends email+password and parses tokens + trainer user', () async {
      t.adapter.onPost(
        ApiEndpoints.login,
        respond: (_) => t.adapter.ok(Samples.authResponse()),
      );

      final result = await t.apiClient.post<AuthResponse>(
        ApiEndpoints.login,
        data: const LoginRequest(
          email: 'coach@test.local',
          password: 'p@ss1234',
        ).toJson(),
        parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
      );

      final req = t.onlyRequest;
      expect(req.method, 'POST');
      expect(req.path, '/api/v1/identity/login');
      expect(req.bodyAsMap, {
        'email': 'coach@test.local',
        'password': 'p@ss1234',
      });
      // Public endpoint — no bearer header should be attached.
      expect(req.header('authorization'), anyOf(isNull, isEmpty));
      expect(result.accessToken, 'new-access-token');
      expect(result.user.id, 'trainer-1');
      expect(result.user.role, 'Trainer');
      expect(result.user.gymMemberships, hasLength(2));
    });

    test('401 surfaces as UnauthorizedException', () async {
      t.adapter.onPost(
        ApiEndpoints.login,
        respond: (_) => t.adapter.error(
          401,
          code: 'INVALID_CREDENTIALS',
          message: 'Wrong email or password',
        ),
      );

      await expectLater(
        t.apiClient.post<AuthResponse>(
          ApiEndpoints.login,
          data: const LoginRequest(email: 'x@y.z', password: 'bad').toJson(),
          parser: (d) => AuthResponse.fromJson(d as Map<String, dynamic>),
        ),
        throwsA(isA<UnauthorizedException>()),
      );
    });
  });

  group('RegisterScreen — POST /identity/register', () {
    test('sends trainer registration payload with PDPL consent', () async {
      t.adapter.onPost(
        ApiEndpoints.register,
        respond: (_) => t.adapter.ok(Samples.authResponse()),
      );

      await t.apiClient.post<AuthResponse>(
        ApiEndpoints.register,
        data: const RegisterRequest(
          email: 'new-coach@test.local',
          password: 'Strong#123',
          firstName: 'Omar',
          lastName: 'Hassan',
          phone: '+201000000001',
          role: 'Trainer',
          dataProcessingConsent: true,
          marketingConsent: false,
        ).toJson(),
        parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['email'], 'new-coach@test.local');
      expect(body['role'], 'Trainer');
      expect(body['dataProcessingConsent'], true);
      expect(body['marketingConsent'], false);
      expect(body['firstName'], 'Omar');
      expect(body['phone'], '+201000000001');
    });

    test('422 validation error maps to ValidationException with field errors',
        () async {
      t.adapter.onPost(
        ApiEndpoints.register,
        respond: (_) => t.adapter.rawResponse(422, {
          'data': null,
          'message': 'Validation failed',
          'errors': [
            {'code': 'Email', 'message': 'Email is already taken'},
            {'code': 'Password', 'message': 'Password must be 8+ chars'},
          ],
        }),
      );

      try {
        await t.apiClient.post<AuthResponse>(
          ApiEndpoints.register,
          data: const RegisterRequest(
            email: 'bad',
            password: 'x',
            firstName: 'a',
            lastName: 'b',
            phone: '1',
            role: 'Trainer',
            dataProcessingConsent: true,
          ).toJson(),
          parser: (d) => AuthResponse.fromJson(d as Map<String, dynamic>),
        );
        fail('expected ValidationException');
      } on ValidationException catch (e) {
        expect(e.fieldErrors['Email'], 'Email is already taken');
        expect(e.fieldErrors['Password'], contains('8+'));
      }
    });
  });

  group('PendingApprovalScreen / AuthNotifier — GET /identity/profile', () {
    test('attaches bearer + X-Gym-Id and parses trainer profile', () async {
      // Authenticated client for this test.
      final authed = buildTestApiClient();
      authed.adapter.onGet(
        ApiEndpoints.profile,
        respond: (_) => authed.adapter.ok(Samples.trainerUser()),
      );

      final user = await authed.apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (d) => UserModel.fromJson(d as Map<String, dynamic>),
      );

      final req = authed.onlyRequest;
      expect(req.method, 'GET');
      expect(req.path, '/api/v1/identity/profile');
      expect(req.header('authorization'), 'Bearer $kAccessToken');
      expect(req.header('x-gym-id'), kGymId);
      expect(user.id, kTrainerId);
      expect(user.role, 'Trainer');
    });

    test('pending trainer returns status="pending" (gym-owner approval flow)',
        () async {
      final authed = buildTestApiClient();
      authed.adapter.onGet(
        ApiEndpoints.profile,
        respond: (_) => authed.adapter.ok({
          ...Samples.trainerUser(),
          'status': 'pending',
        }),
      );

      final user = await authed.apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (d) => UserModel.fromJson(d as Map<String, dynamic>),
      );
      expect(user.status, 'pending');
    });
  });
}
