import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers AvailabilityScreen, BookingRequestsScreen, CalendarScreen,
/// RecurringSessionScreen, LeaveManagementScreen.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('AvailabilityScreen', () {
    test('GET /scheduling/availability parses weeklySlots envelope', () async {
      t.adapter.onGet(
        ApiEndpoints.trainerAvailability,
        respond: (_) => t.adapter.ok({
          'weeklySlots': [
            {
              'id': 'a1',
              'trainerId': 'trainer-1',
              'slots': [
                {
                  'id': 'slot-1',
                  'dayOfWeek': 1,
                  'startTime': '09:00',
                  'endTime': '12:00',
                  'isActive': true,
                },
              ],
              'bufferMinutes': 15,
            },
          ],
          'blockedDates': [],
        }),
      );

      final availability =
          await t.apiClient.get<List<TrainerAvailabilityModel>>(
        ApiEndpoints.trainerAvailability,
        queryParameters: {'trainerId': kTrainerId, 'gymId': kGymId},
        parser: (data) {
          final map = data as Map<String, dynamic>;
          final slots = map['weeklySlots'] as List<dynamic>;
          return slots
              .map((e) =>
                  TrainerAvailabilityModel.fromJson(e as Map<String, dynamic>))
              .toList();
        },
      );

      final req = t.onlyRequest;
      expect(req.method, 'GET');
      expect(req.path, '/api/v1/scheduling/availability');
      expect(req.q('trainerId'), kTrainerId);
      expect(req.q('gymId'), kGymId);
      expect(availability.single.bufferMinutes, 15);
      expect(availability.single.slots.single.dayOfWeek, 1);
    });

    test('PUT /scheduling/availability saves the full weekly grid', () async {
      t.adapter.onPut(
        ApiEndpoints.trainerAvailability,
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.putVoid(
        ApiEndpoints.trainerAvailability,
        data: {
          'trainerId': kTrainerId,
          'gymId': kGymId,
          'sessionDurationMinutes': 60,
          'bufferMinutes': 15,
          'weeklySlots': [
            {
              'dayOfWeek': 1,
              'startTime': '09:00:00',
              'endTime': '12:00:00',
            },
          ],
        },
      );

      final req = t.onlyRequest;
      expect(req.method, 'PUT');
      expect(req.path, '/api/v1/scheduling/availability');
      expect(req.bodyAsMap!['trainerId'], kTrainerId);
      expect(req.bodyAsMap!['sessionDurationMinutes'], 60);
      expect((req.bodyAsMap!['weeklySlots'] as List).single['startTime'],
          '09:00:00');
    });
  });

  group('BookingRequestsScreen', () {
    test('GET /scheduling/sessions?status=pending for trainer', () async {
      t.adapter.onGet(
        ApiEndpoints.sessions,
        respond: (_) => t.adapter.ok([
          Samples.session(id: 'pending-1', status: 'Pending'),
        ]),
      );

      final pending = await t.apiClient.get<List<SessionModel>>(
        ApiEndpoints.sessions,
        queryParameters: {
          'gymId': kGymId,
          'trainerId': kTrainerId,
          'status': 'pending',
          'page': 1,
          'pageSize': 50,
        },
        parser: (data) => (data as List<dynamic>)
            .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      expect(t.onlyRequest.q('status'), 'pending');
      expect(pending.single.status, 'Pending');
    });

    test('POST /scheduling/sessions/{id}/confirm accepts a booking', () async {
      t.adapter.onPost(
        '${ApiEndpoints.sessionById('session-1')}/confirm',
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient
          .postVoid('${ApiEndpoints.sessionById('session-1')}/confirm');

      final req = t.onlyRequest;
      expect(req.method, 'POST');
      expect(req.path, '/api/v1/scheduling/sessions/session-1/confirm');
    });

    test('POST /scheduling/sessions/{id}/cancel declines with reason',
        () async {
      t.adapter.onPost(
        '${ApiEndpoints.sessionById('session-1')}/cancel',
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.postVoid(
        '${ApiEndpoints.sessionById('session-1')}/cancel',
        data: {'cancelledBy': kTrainerId, 'reason': 'declined by trainer'},
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['cancelledBy'], kTrainerId);
      expect(body['reason'], 'declined by trainer');
    });
  });

  group('CalendarScreen — upcoming sessions', () {
    test('GET /scheduling/sessions/upcoming with role=trainer', () async {
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
      expect(req.path, '/api/v1/scheduling/sessions/upcoming');
      expect(req.q('role'), 'trainer');
      expect(sessions.single.durationMinutes, 60);
    });
  });

  group('RecurringSessionScreen', () {
    test('POST /scheduling/sessions/recurring sends full payload', () async {
      t.adapter.onPost(
        ApiEndpoints.recurringSessions,
        respond: (_) => t.adapter.created({'id': 'rec-1'}),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.recurringSessions,
        data: {
          'gymId': kGymId,
          'trainerId': kTrainerId,
          'memberId': kClientId,
          'dayOfWeek': 1,
          'time': '10:00:00',
          'durationMinutes': 60,
          'sessionType': 'PersonalTraining',
          'weeksAhead': 8,
        },
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['memberId'], kClientId);
      expect(body['sessionType'], 'PersonalTraining');
      expect(body['weeksAhead'], 8);
      expect(body['dayOfWeek'], 1);
    });
  });

  group('LeaveManagementScreen', () {
    test('GET /scheduling/availability returns blockedDates array', () async {
      t.adapter.onGet(
        ApiEndpoints.trainerAvailability,
        respond: (_) => t.adapter.ok({
          'weeklySlots': [],
          'blockedDates': ['2026-04-25', '2026-04-26'],
        }),
      );

      final dates = await t.apiClient.get<List<String>>(
        ApiEndpoints.trainerAvailability,
        queryParameters: {'trainerId': kTrainerId, 'gymId': kGymId},
        parser: (data) {
          final blocked = (data as Map<String, dynamic>)['blockedDates'];
          return (blocked as List<dynamic>).map((e) => e.toString()).toList();
        },
      );

      expect(dates, ['2026-04-25', '2026-04-26']);
    });

    test('POST /scheduling/availability/block-date for each day in range',
        () async {
      t.adapter.onPost(
        ApiEndpoints.blockDate,
        respond: (_) => t.adapter.noContent(),
      );

      for (final day in ['2026-04-25', '2026-04-26']) {
        await t.apiClient.postVoid(
          ApiEndpoints.blockDate,
          data: {
            'trainerId': kTrainerId,
            'gymId': kGymId,
            'date': day,
            'autoNotifyClients': true,
          },
        );
      }

      expect(t.adapter.recorded, hasLength(2));
      expect(t.adapter.recorded.first.path,
          '/api/v1/scheduling/availability/block-date');
      expect(
        t.adapter.recorded.map((r) => r.bodyAsMap!['date']).toList(),
        ['2026-04-25', '2026-04-26'],
      );
      expect(t.adapter.recorded.first.bodyAsMap!['autoNotifyClients'], true);
    });
  });
}
