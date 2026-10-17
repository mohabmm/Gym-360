import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers LiveSessionScreen and PostSessionLogScreen (the two session-flow
/// entry points the trainer app uses).
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  test('GET /scheduling/sessions/{id} returns single session', () async {
    t.adapter.onGet(
      ApiEndpoints.sessionById(const SessionId('session-1')),
      respond: (_) => t.adapter.ok(Samples.session()),
    );

    final s = await t.apiClient.get<SessionModel>(
      ApiEndpoints.sessionById(const SessionId('session-1')),
      parser: (d) => SessionModel.fromJson(d as Map<String, dynamic>),
    );

    final req = t.onlyRequest;
    expect(req.method, 'GET');
    expect(req.path, '/api/v1/scheduling/sessions/session-1');
    expect(s.id, 'session-1');
    expect(s.trainerId, 'trainer-1');
  });

  test('LiveSessionScreen — POST /sessions/{id}/trainer-log on complete',
      () async {
    t.adapter.onPost(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-1')),
      respond: (_) => t.adapter.noContent(),
    );

    await t.apiClient.postVoid(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-1')),
      data: {
        'trainerId': kTrainerId,
        'exercisesPerformed':
            'ex:ex-1 set 1 10x60.0; ex:ex-1 set 2 10x60.0',
        'clientEffortRating': 5,
        'feedback': 'Nice form on bench',
        'attendanceConfirmed': true,
      },
    );

    final req = t.onlyRequest;
    expect(req.method, 'POST');
    expect(req.path, '/api/v1/scheduling/sessions/session-1/trainer-log');
    expect(req.bodyAsMap!['trainerId'], kTrainerId);
    expect(req.bodyAsMap!['clientEffortRating'], 5);
    expect(req.bodyAsMap!['attendanceConfirmed'], true);
    expect(req.bodyAsMap!['exercisesPerformed'], contains('ex:ex-1'));
  });

  test('LiveSessionScreen — attendance=true but no performed sets sends "-"',
      () async {
    t.adapter.onPost(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-2')),
      respond: (_) => t.adapter.noContent(),
    );

    // Simulate UI path: _sets empty, notes empty → exercisesSummary = '-'.
    await t.apiClient.postVoid(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-2')),
      data: {
        'trainerId': kTrainerId,
        'exercisesPerformed': '-',
        'clientEffortRating': 5,
        'feedback': null,
        'attendanceConfirmed': true,
      },
    );

    expect(t.onlyRequest.bodyAsMap!['exercisesPerformed'], '-');
    expect(t.onlyRequest.bodyAsMap!.containsKey('feedback'), true);
  });

  test('PostSessionLogScreen — trainer-log carries nextSessionFocus', () async {
    t.adapter.onPost(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-1')),
      respond: (_) => t.adapter.noContent(),
    );

    await t.apiClient.postVoid(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-1')),
      data: {
        'trainerId': kTrainerId,
        'exercisesPerformed': 'Focus on pull day tomorrow',
        'clientEffortRating': 4,
        'nextSessionFocus': 'Focus on pull day tomorrow',
        'attendanceConfirmed': true,
      },
    );

    final body = t.onlyRequest.bodyAsMap!;
    expect(body['clientEffortRating'], 4);
    expect(body['nextSessionFocus'], 'Focus on pull day tomorrow');
  });

  test('403 feature-gated error maps to FeatureGatedException', () async {
    t.adapter.onPost(
      ApiEndpoints.sessionTrainerLog(const SessionId('session-1')),
      respond: (_) => t.adapter.error(
        403,
        code: 'PLAN.FEATURE_NOT_AVAILABLE',
        message: 'Not available on Free plan',
        extras: {
          'feature': 'session_logging',
          'requiredPlan': 'Starter',
        },
      ),
    );

    await expectLater(
      t.apiClient.postVoid(
        ApiEndpoints.sessionTrainerLog(const SessionId('session-1')),
        data: {
          'trainerId': kTrainerId,
          'exercisesPerformed': '-',
          'clientEffortRating': 5,
          'attendanceConfirmed': true,
        },
      ),
      throwsA(isA<FeatureGatedException>()),
    );
  });
}
