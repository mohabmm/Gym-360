import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/scheduling/data/datasources/scheduling_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Scheduling — upcoming, sessions list, book session, cancel, member feedback,
/// available slots, classes list, book/cancel class.
void main() {
  late TestApiClient t;
  late SchedulingRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = SchedulingRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET upcoming sessions', () async {
    t.adapter.onGet(
      ApiEndpoints.upcomingSessions,
      respond: (_) => t.adapter.ok([Samples.session()]),
    );
    final list =
        await ds.getUpcomingSessions(userId: 'user-1', gymId: 'gym-1');
    expect(list.first.durationMinutes, 60);
    expect(t.onlyRequest.q('role'), 'member');
  });

  test('GET /scheduling/sessions with filters', () async {
    t.adapter.onGet(
      ApiEndpoints.sessions,
      respond: (_) => t.adapter.ok([Samples.session()]),
    );
    await ds.getAllSessions(
      gymId: 'gym-1',
      memberId: 'user-1',
      status: 'Scheduled',
      page: 3,
      pageSize: 15,
    );
    final q = t.onlyRequest.query;
    expect(q['memberId']!.first, 'user-1');
    expect(q['status']!.first, 'Scheduled');
    expect(q['page']!.first, '3');
    expect(q['pageSize']!.first, '15');
  });

  test('GET /scheduling/sessions/{id} returns single session', () async {
    t.adapter.onGet(
      ApiEndpoints.sessionById('session-1'),
      respond: (_) => t.adapter.ok(Samples.session()),
    );
    final s = await ds.getSessionById('session-1');
    expect(s.trainerName, 'Coach Omar');
  });

  test('POST /scheduling/sessions books a session', () async {
    t.adapter.onPost(
      ApiEndpoints.sessions,
      respond: (_) => t.adapter.created('session-new'),
    );
    final id = await ds.bookSession(
      const BookSessionRequest(
        trainerId: 'trainer-1',
        date: '2026-04-20',
        time: '09:00',
        durationMinutes: 60,
        sessionType: 'PT',
      ),
    );
    expect(id, 'session-new');
    expect(t.onlyRequest.bodyAsMap!['durationMinutes'], 60);
  });

  test('Feature-gated booking surfaces FeatureGatedException', () async {
    t.adapter.onPost(
      ApiEndpoints.sessions,
      respond: (_) => t.adapter.rawResponse(403, {
        'data': null,
        'errors': [
          {'code': 'FEATURE_GATED', 'message': 'Upgrade to book trainers'},
        ],
        'errorCode': 'FEATURE_GATED',
        'featureCode': 'BOOK_TRAINER_SESSIONS',
        'requiredPlan': 'Pro',
      }),
    );
    await expectLater(
      ds.bookSession(
        const BookSessionRequest(
          trainerId: 't',
          date: '2026-04-20',
          time: '09:00',
          durationMinutes: 60,
          sessionType: 'PT',
        ),
      ),
      throwsA(isA<FeatureGatedException>()
          .having((e) => e.featureCode, 'featureCode', 'BOOK_TRAINER_SESSIONS')
          .having((e) => e.requiredPlan, 'requiredPlan', 'Pro')),
    );
  });

  test('POST /scheduling/sessions/{id}/cancel sends cancelledBy + reason',
      () async {
    t.adapter.onPost(
      '${ApiEndpoints.sessionById('session-1')}/cancel',
      respond: (_) => t.adapter.noContent(),
    );
    await ds.cancelSession(
      'session-1',
      cancelledBy: 'user-1',
      reason: 'Feeling unwell',
    );
    expect(t.onlyRequest.bodyAsMap, {
      'cancelledBy': 'user-1',
      'reason': 'Feeling unwell',
    });
  });

  test('POST /scheduling/sessions/{id}/member-log stores member feedback',
      () async {
    t.adapter.onPost(
      ApiEndpoints.sessionMemberLog('session-1'),
      respond: (_) => t.adapter.noContent(),
    );
    await ds.logMemberFeedback('session-1', {
      'rating': 5,
      'rpe': 8,
      'notes': 'Strong session',
    });
    expect(t.onlyRequest.bodyAsMap!['rpe'], 8);
  });

  test('GET /scheduling/available-slots requires trainerId + gymId + date',
      () async {
    t.adapter.onGet(
      ApiEndpoints.availableSlots,
      respond: (_) => t.adapter.ok([Samples.availabilitySlot()]),
    );
    await ds.getAvailabilitySlots(
      'trainer-1',
      'gym-1',
      DateTime.utc(2026, 4, 20),
    );
    expect(t.onlyRequest.q('trainerId'), 'trainer-1');
    expect(t.onlyRequest.q('gymId'), 'gym-1');
    expect(t.onlyRequest.q('date'), '2026-04-20');
  });

  test('GET classes for a gym on a date', () async {
    t.adapter.onGet(
      ApiEndpoints.gymClasses('gym-1'),
      respond: (_) => t.adapter.ok([Samples.gymClass()]),
    );
    final classes =
        await ds.getClasses(gymId: 'gym-1', date: DateTime.utc(2026, 4, 17));
    expect(classes.first.capacity, 20);
    expect(t.onlyRequest.q('date'), '2026-04-17');
  });

  test('POST book class sends memberId', () async {
    t.adapter.onPost(
      ApiEndpoints.bookGymClass('gym-1', 'class-1'),
      respond: (_) => t.adapter.noContent(),
    );
    await ds.bookClass(
      gymId: 'gym-1',
      classId: 'class-1',
      memberId: 'user-1',
    );
    expect(t.onlyRequest.bodyAsMap, {'memberId': 'user-1'});
  });

  test('DELETE class booking', () async {
    t.adapter.onDelete(
      ApiEndpoints.cancelGymClassBooking('gym-1', 'class-1', 'book-1'),
      respond: (_) => t.adapter.noContent(),
    );
    await ds.cancelClassBooking(
      gymId: 'gym-1',
      classId: 'class-1',
      bookingId: 'book-1',
    );
    expect(t.onlyRequest.method, 'DELETE');
  });
}
