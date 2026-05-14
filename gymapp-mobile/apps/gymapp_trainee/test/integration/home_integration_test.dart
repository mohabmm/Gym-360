import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/home/data/datasources/home_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Home screen pulls from four endpoints in parallel:
/// today's workout (gated on remote mode), upcoming sessions, progress
/// dashboard, today's water. These tests ensure each call goes to the right
/// place with the right query params and that the 404 no-workout case is
/// swallowed rather than bubbling up to the UI.
void main() {
  late TestApiClient t;
  late HomeRemoteDataSource home;

  setUp(() {
    t = buildTestApiClient();
    home = HomeRemoteDataSource(apiClient: t.apiClient);
  });

  test("GET /training/todays-workout returns parsed workout", () async {
    t.adapter.onGet(
      ApiEndpoints.todaysWorkout,
      respond: (_) => t.adapter.ok(Samples.todaysWorkoutResponse()),
    );

    final w = await home.getTodaysWorkout(userId: 'user-1', gymId: 'gym-1');
    expect(w, isNotNull);
    expect(w!.id, 'workout-1');
    expect(w.name, 'Push Day A');
    expect(w.targetMuscleGroups, containsAll(['Chest', 'Triceps']));
    expect(t.onlyRequest.q('userId'), 'user-1');
    expect(t.onlyRequest.q('gymId'), 'gym-1');
  });

  test("GET /training/todays-workout 404 is treated as 'no workout today'",
      () async {
    t.adapter.onGet(
      ApiEndpoints.todaysWorkout,
      respond: (_) => t.adapter.error(404, code: 'NOT_FOUND'),
    );
    expect(
      await home.getTodaysWorkout(userId: 'user-1', gymId: 'gym-1'),
      isNull,
    );
  });

  test("GET /scheduling/sessions/upcoming carries role=member", () async {
    t.adapter.onGet(
      ApiEndpoints.upcomingSessions,
      respond: (_) => t.adapter.ok([Samples.session()]),
    );

    final sessions =
        await home.getUpcomingSessions(userId: 'user-1', gymId: 'gym-1');
    expect(sessions, hasLength(1));
    expect(sessions.first.trainerId, 'trainer-1');
    expect(t.onlyRequest.q('role'), 'member');
  });

  test('GET /progress/dashboard parses latestMetrics + streaks', () async {
    t.adapter.onGet(
      ApiEndpoints.progressDashboard,
      respond: (_) => t.adapter.ok(Samples.progressDashboard()),
    );

    final d =
        await home.getProgressDashboard(userId: 'user-1', gymId: 'gym-1');
    expect(d.currentWorkoutStreak, 4);
    expect(d.latestMetrics?.weight, 72.5);
  });

  test('GET /nutrition/water-log includes today date', () async {
    t.adapter.onGet(
      ApiEndpoints.waterLog,
      respond: (_) => t.adapter.ok(Samples.waterLog()),
    );

    final w = await home.getTodaysWater(userId: 'user-1', gymId: 'gym-1');
    expect(w.totalMl, 1500);
    // Date comes from DateTime.now() so we only check the format is YYYY-MM-DD.
    final date = t.onlyRequest.q('date')!;
    expect(RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(date), isTrue);
  });
}
