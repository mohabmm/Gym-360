import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers SettingsScreen (PDPL + account), ProfileScreen (self-update),
/// GymSwitcherScreen, and the plan-features + gym fetches used by the shell.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('ProfileScreen', () {
    test('PUT /identity/profile updates name+phone', () async {
      t.adapter.onPut(
        ApiEndpoints.profile,
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.putVoid(
        ApiEndpoints.profile,
        data: {
          'userId': kTrainerId,
          'firstName': 'Omar',
          'lastName': 'Hassan',
          'phone': '+201000000001',
        },
      );

      final req = t.onlyRequest;
      expect(req.method, 'PUT');
      expect(req.path, '/api/v1/identity/profile');
      expect(req.bodyAsMap, {
        'userId': kTrainerId,
        'firstName': 'Omar',
        'lastName': 'Hassan',
        'phone': '+201000000001',
      });
      expect(req.header('authorization'), 'Bearer $kAccessToken');
      expect(req.header('x-gym-id'), kGymId);
    });

    test('GET /identity/profile refetches after update', () async {
      t.adapter.onGet(
        ApiEndpoints.profile,
        respond: (_) => t.adapter.ok(Samples.trainerUser()),
      );

      final user = await t.apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (d) => UserModel.fromJson(d as Map<String, dynamic>),
      );
      expect(user.id, kTrainerId);
      expect(user.role, 'Trainer');
    });
  });

  group('PDPL — SettingsScreen', () {
    test('GET /identity/my-data triggers async data export', () async {
      t.adapter.onGet(
        ApiEndpoints.myData,
        respond: (_) => t.adapter.ok({
          'status': 'queued',
          'estimatedEmailMinutes': 15,
        }),
      );

      final data = await t.apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.myData,
        parser: (d) => d as Map<String, dynamic>,
      );

      final req = t.onlyRequest;
      expect(req.method, 'GET');
      expect(req.path, '/api/v1/identity/my-data');
      expect(data['status'], 'queued');
    });

    test('DELETE /identity/account triggers account deletion', () async {
      t.adapter.onDelete(
        ApiEndpoints.deleteAccount,
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.delete(ApiEndpoints.deleteAccount);

      final req = t.onlyRequest;
      expect(req.method, 'DELETE');
      expect(req.path, '/api/v1/identity/account');
      expect(req.header('authorization'), 'Bearer $kAccessToken');
    });

    test('POST /identity/consent records a marketing consent', () async {
      t.adapter.onPost(
        ApiEndpoints.consent,
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.consent,
        data: {
          'consentType': 'Marketing',
          'isGranted': true,
          'consentText': 'v1',
        },
      );

      expect(t.onlyRequest.bodyAsMap, {
        'consentType': 'Marketing',
        'isGranted': true,
        'consentText': 'v1',
      });
    });
  });

  group('GymSwitcherScreen / shell', () {
    test('GET /gyms/{gymId} fetches gym details for membership', () async {
      t.adapter.onGet(
        ApiEndpoints.gymById(kGymId),
        respond: (_) => t.adapter.ok(Samples.gym()),
      );

      final gym = await t.apiClient.get<GymModel>(
        ApiEndpoints.gymById(kGymId),
        parser: (d) => GymModel.fromJson(d as Map<String, dynamic>),
      );

      expect(t.onlyRequest.path, '/api/v1/gyms/$kGymId');
      expect(gym.id, kGymId);
      expect(gym.name, 'Tameeni Test Gym');
    });
  });

  group('Plan features', () {
    test('GET /billing/plan-features returns the active plan envelope',
        () async {
      t.adapter.onGet(
        ApiEndpoints.planFeatures,
        respond: (_) => t.adapter.ok(Samples.planFeatures()),
      );

      final plan = await t.apiClient.get<PlanFeaturesModel>(
        ApiEndpoints.planFeatures,
        parser: (d) => PlanFeaturesModel.fromJson(d as Map<String, dynamic>),
      );

      final req = t.onlyRequest;
      expect(req.path, '/api/v1/billing/plan-features');
      expect(req.header('x-gym-id'), kGymId);
      expect(plan.planCode, 'pro');
      expect(plan.isEnabled('remote_workouts'), true);
      expect(plan.memberLimit, 500);
    });

    test('GymSuspendedException fires when BE returns GYM_SUSPENDED',
        () async {
      t.adapter.onGet(
        ApiEndpoints.planFeatures,
        respond: (_) => t.adapter.error(
          403,
          code: 'GYM_SUSPENDED',
          message: 'Gym subscription is suspended',
        ),
      );

      await expectLater(
        t.apiClient.get<PlanFeaturesModel>(
          ApiEndpoints.planFeatures,
          parser: (d) => PlanFeaturesModel.fromJson(d as Map<String, dynamic>),
        ),
        throwsA(isA<GymSuspendedException>()),
      );
    });
  });

  group('Interceptor contracts', () {
    test('Authorization + X-Gym-Id + Accept-Language=ar attached on every call',
        () async {
      t.adapter.onGet(
        ApiEndpoints.profile,
        respond: (_) => t.adapter.ok(Samples.trainerUser()),
      );

      await t.apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (d) => UserModel.fromJson(d as Map<String, dynamic>),
      );

      final req = t.onlyRequest;
      expect(req.header('authorization'), 'Bearer $kAccessToken');
      expect(req.header('x-gym-id'), kGymId);
      expect(req.header('accept-language'), 'ar');
      expect(req.header('content-type'), startsWith('application/json'));
    });
  });
}
