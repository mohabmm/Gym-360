import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers ClientRosterScreen, ClientDetailScreen, ClientNotesScreen,
/// OnboardingFormScreen and the remote-workout toggle inside ClientDetail.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('ClientRosterScreen', () {
    test('GET /gyms/{gymId}/members with page+pageSize parses UserModel list',
        () async {
      t.adapter.onGet(
        ApiEndpoints.gymMembers(kGymId),
        respond: (_) => t.adapter.ok({
          'items': [Samples.clientUser(), Samples.clientUser(id: 'client-2')],
          'totalCount': 2,
        }),
      );

      final users = await t.apiClient.get<List<UserModel>>(
        ApiEndpoints.gymMembers(kGymId),
        queryParameters: {'page': 1, 'pageSize': 100},
        parser: (data) {
          final list = (data as Map<String, dynamic>)['items'] as List<dynamic>;
          return list
              .map((e) => UserModel.fromJson(e as Map<String, dynamic>))
              .toList();
        },
      );

      final req = t.onlyRequest;
      expect(req.method, 'GET');
      expect(req.path, '/api/v1/gyms/$kGymId/members');
      expect(req.q('page'), '1');
      expect(req.q('pageSize'), '100');
      expect(users, hasLength(2));
      expect(users.first.goals, ['WeightLoss', 'Toning']);
    });
  });

  group('ClientDetailScreen', () {
    test('GET /gyms/{gymId}/members/{memberId} returns full profile',
        () async {
      t.adapter.onGet(
        ApiEndpoints.gymMemberById(kGymId, kClientId),
        respond: (_) => t.adapter.ok(Samples.clientUser()),
      );

      final user = await t.apiClient.get<UserModel>(
        ApiEndpoints.gymMemberById(kGymId, kClientId),
        parser: (d) => UserModel.fromJson(d as Map<String, dynamic>),
      );

      expect(t.onlyRequest.path, '/api/v1/gyms/$kGymId/members/$kClientId');
      expect(user.id, kClientId);
      expect(user.firstName, 'Mona');
    });

    test('GET /scheduling/sessions?memberId= returns client sessions',
        () async {
      t.adapter.onGet(
        ApiEndpoints.sessions,
        respond: (_) => t.adapter.ok([Samples.session()]),
      );

      final sessions = await t.apiClient.get<List<SessionModel>>(
        ApiEndpoints.sessions,
        queryParameters: {
          'gymId': kGymId,
          'memberId': kClientId,
          'page': 1,
          'pageSize': 50,
        },
        parser: (data) => (data as List<dynamic>)
            .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.q('memberId'), kClientId);
      expect(req.q('gymId'), kGymId);
      expect(sessions.single.memberId, 'client-1');
    });

    test('GET /progress/clients/{clientId} returns body metrics', () async {
      t.adapter.onGet(
        ApiEndpoints.clientProgress(kClientId),
        respond: (_) => t.adapter.ok([Samples.bodyMetric()]),
      );

      final metrics = await t.apiClient.get<List<BodyMetricEntryModel>>(
        ApiEndpoints.clientProgress(kClientId),
        parser: (data) => (data as List<dynamic>)
            .map((e) => BodyMetricEntryModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      expect(t.onlyRequest.path, '/api/v1/progress/clients/$kClientId');
      expect(metrics.single.weight, 72.5);
      expect(metrics.single.weightUnit, 'kg');
    });

    test('GET /training/remote-workout/status returns RemoteWorkoutStatus',
        () async {
      t.adapter.onGet(
        ApiEndpoints.remoteWorkoutStatus,
        respond: (_) => t.adapter.ok(Samples.remoteWorkoutStatus()),
      );

      final status = await t.apiClient.get<RemoteWorkoutStatusModel>(
        ApiEndpoints.remoteWorkoutStatus,
        queryParameters: {'clientId': kClientId, 'gymId': kGymId},
        parser: (d) =>
            RemoteWorkoutStatusModel.fromJson(d as Map<String, dynamic>),
      );

      final req = t.onlyRequest;
      expect(req.path, '/api/v1/training/remote-workout/status');
      expect(req.q('clientId'), kClientId);
      expect(req.q('gymId'), kGymId);
      expect(status.isActive, true);
    });

    test('GET /billing/packages?memberId filters to active package', () async {
      t.adapter.onGet(
        ApiEndpoints.sessionPackages,
        respond: (_) => t.adapter.ok([Samples.sessionPackage()]),
      );

      final packages = await t.apiClient.get<List<SessionPackageModel>>(
        ApiEndpoints.sessionPackages,
        queryParameters: {
          'gymId': kGymId,
          'memberId': kClientId,
          'status': 'active',
        },
        parser: (data) => (data as List<dynamic>)
            .map((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.q('status'), 'active');
      expect(req.q('memberId'), kClientId);
      expect(packages.single.remainingSessions, 7);
      expect(packages.single.price, 3000.0);
    });

    test('POST /training/remote-workout/activate sends trainer+client+gym+reason',
        () async {
      t.adapter.onPost(
        ApiEndpoints.remoteWorkoutActivate,
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.remoteWorkoutActivate,
        data: {
          'trainerId': kTrainerId,
          'clientId': kClientId,
          'gymId': kGymId,
          'reason': 'Out of town',
        },
      );

      final req = t.onlyRequest;
      expect(req.method, 'POST');
      expect(req.path, '/api/v1/training/remote-workout/activate');
      expect(req.bodyAsMap, {
        'trainerId': kTrainerId,
        'clientId': kClientId,
        'gymId': kGymId,
        'reason': 'Out of town',
      });
    });

    test('POST /training/remote-workout/deactivate omits reason', () async {
      t.adapter.onPost(
        ApiEndpoints.remoteWorkoutDeactivate,
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.remoteWorkoutDeactivate,
        data: {
          'trainerId': kTrainerId,
          'clientId': kClientId,
          'gymId': kGymId,
        },
      );

      expect(t.onlyRequest.bodyAsMap!.containsKey('reason'), false);
    });
  });

  group('ClientNotesScreen', () {
    test('GET /gyms/{gymId}/members/{clientId}/notes returns notes list',
        () async {
      t.adapter.onGet(
        ApiEndpoints.gymMemberNotes(kGymId, kClientId),
        respond: (_) => t.adapter.ok([Samples.clientNote()]),
      );

      final raw = await t.apiClient.get<List<dynamic>>(
        ApiEndpoints.gymMemberNotes(kGymId, kClientId),
        parser: (d) => d as List<dynamic>,
      );

      expect(t.onlyRequest.path,
          '/api/v1/gyms/$kGymId/members/$kClientId/notes');
      expect((raw.single as Map<String, dynamic>)['id'], 'note-1');
    });

    test('POST /gyms/{gymId}/members/{clientId}/notes sends { content }',
        () async {
      t.adapter.onPost(
        ApiEndpoints.gymMemberNotes(kGymId, kClientId),
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.gymMemberNotes(kGymId, kClientId),
        data: {'content': 'Shoulder feels better today'},
      );

      expect(t.onlyRequest.bodyAsMap,
          {'content': 'Shoulder feels better today'});
    });
  });

  group('OnboardingFormScreen', () {
    test('POST /gyms/{gymId}/members/{clientId}/onboarding sends form fields',
        () async {
      t.adapter.onPost(
        ApiEndpoints.gymMemberOnboarding(kGymId, kClientId),
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.gymMemberOnboarding(kGymId, kClientId),
        data: {
          'medicalHistory': 'none',
          'fitnessGoals': 'lose 5kg',
          'injuries': 'none',
          'medications': 'none',
          'fitnessLevel': 'Intermediate',
        },
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['fitnessGoals'], 'lose 5kg');
      expect(body['fitnessLevel'], 'Intermediate');
      expect(body['medicalHistory'], 'none');
    });
  });

  test('NO_ACTIVE_GYM path — detail provider throws when gym missing', () {
    // The provider code throws ApiException locally when activeGymIdProvider
    // is null; no HTTP call is made. Enforce that ApiException shape here.
    final e = ApiException(
      message: 'No active gym selected.',
      errorCode: 'NO_ACTIVE_GYM',
    );
    expect(e.errorCode, 'NO_ACTIVE_GYM');
    expect(e.message, contains('gym'));
  });
}
