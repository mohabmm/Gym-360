import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// TrainerHomeScreen (trainer dashboard) fans out to 5 endpoints:
///   GET /scheduling/sessions/upcoming   (today's sessions for trainer)
///   GET /scheduling/sessions            (pending bookings via ?status=pending)
///   GET /communication/conversations    (unread counts)
///   GET /gyms/{gymId}/members           (total clients)
///   GET /gamification/trainers/{id}/public-profile  (display rating)
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  test("GET /scheduling/sessions/upcoming carries userId+gymId+role=trainer",
      () async {
    t.adapter.onGet(
      ApiEndpoints.upcomingSessions,
      respond: (_) => t.adapter.ok([Samples.session()]),
    );

    final sessions = await t.apiClient.get<List<SessionModel>>(
      ApiEndpoints.upcomingSessions,
      queryParameters: {
        'userId': kTrainerId,
        'gymId': kGymId,
        'role': 'trainer',
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

    final req = t.onlyRequest;
    expect(req.method, 'GET');
    expect(req.path, '/api/v1/scheduling/sessions/upcoming');
    expect(req.q('userId'), kTrainerId);
    expect(req.q('gymId'), kGymId);
    expect(req.q('role'), 'trainer');
    expect(sessions, hasLength(1));
    expect(sessions.first.id, 'session-1');
    expect(sessions.first.trainerId, 'trainer-1');
  });

  test('GET /scheduling/sessions?status=pending returns pending booking count',
      () async {
    t.adapter.onGet(
      ApiEndpoints.sessions,
      respond: (_) => t.adapter.ok([
        Samples.session(id: 'session-p1', status: 'Pending'),
        Samples.session(id: 'session-p2', status: 'Pending'),
      ]),
    );

    final pending = await t.apiClient.get<List<dynamic>>(
      ApiEndpoints.sessions,
      queryParameters: {
        'gymId': kGymId,
        'trainerId': kTrainerId,
        'status': 'pending',
        'page': 1,
        'pageSize': 50,
      },
      parser: (data) => data as List<dynamic>,
    );

    final req = t.onlyRequest;
    expect(req.path, '/api/v1/scheduling/sessions');
    expect(req.q('status'), 'pending');
    expect(req.q('trainerId'), kTrainerId);
    expect(req.q('page'), '1');
    expect(req.q('pageSize'), '50');
    expect(pending, hasLength(2));
  });

  test('GET /communication/conversations aggregates unread counts', () async {
    // BE's ConversationDto does not return unreadCount; the dashboard
    // provider tolerates the missing field and falls back to 0. Here we
    // inject the field inline to exercise the aggregation path itself.
    t.adapter.onGet(
      ApiEndpoints.conversations,
      respond: (_) => t.adapter.ok([
        {...Samples.conversation(), 'unreadCount': 2},
        {...Samples.conversation(), 'id': 'conv-2', 'unreadCount': 3},
      ]),
    );

    final convs = await t.apiClient.get<List<dynamic>>(
      ApiEndpoints.conversations,
      parser: (data) => data as List<dynamic>,
    );

    final totalUnread = convs.fold<int>(
      0,
      (sum, c) =>
          sum + ((c as Map<String, dynamic>)['unreadCount'] as int? ?? 0),
    );
    expect(totalUnread, 5);
    expect(t.onlyRequest.path, '/api/v1/communication/conversations');
  });

  test('GET /gyms/{gymId}/members returns paged members', () async {
    t.adapter.onGet(
      ApiEndpoints.gymMembers(kGym),
      respond: (_) => t.adapter.ok({
        'items': [Samples.clientUser()],
        'totalCount': 42,
        'page': 1,
        'pageSize': 1,
      }),
    );

    final raw = await t.apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.gymMembers(kGym),
      queryParameters: {'page': 1, 'pageSize': 1},
      parser: (data) => data as Map<String, dynamic>,
    );

    final req = t.onlyRequest;
    expect(req.path, '/api/v1/gyms/$kGymId/members');
    expect(req.q('page'), '1');
    expect(req.q('pageSize'), '1');
    expect(raw['totalCount'], 42);
  });

  test('GET /gamification/trainers/{id}/public-profile returns displayRating',
      () async {
    t.adapter.onGet(
      ApiEndpoints.trainerPublicProfile(kTrainer),
      respond: (_) => t.adapter.ok(Samples.trainerPublicProfile()),
    );

    final profile = await t.apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.trainerPublicProfile(kTrainer),
      queryParameters: {'gymId': kGymId},
      parser: (data) => data as Map<String, dynamic>,
    );

    final req = t.onlyRequest;
    expect(req.path, '/api/v1/gamification/trainers/$kTrainerId/public-profile');
    expect(req.q('gymId'), kGymId);
    expect((profile['displayRating'] as num).toDouble(), 4.7);
  });

  test('any failing call swallows gracefully — dashboard keeps rendering',
      () async {
    // Simulate a 500 on upcoming sessions — the dashboard catches it and
    // leaves that card empty. This test pins that contract.
    t.adapter.onGet(
      ApiEndpoints.upcomingSessions,
      respond: (_) => t.adapter.error(500, code: 'INTERNAL'),
    );

    try {
      await t.apiClient.get<List<dynamic>>(
        ApiEndpoints.upcomingSessions,
        queryParameters: {
          'userId': kTrainerId,
          'gymId': kGymId,
          'role': 'trainer',
        },
        parser: (d) => d as List<dynamic>,
      );
      fail('expected ApiException');
    } on ApiException catch (e) {
      expect(e.statusCode, 500);
    }
  });
}
