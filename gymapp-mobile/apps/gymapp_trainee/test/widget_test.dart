import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/home/data/datasources/home_remote_datasource.dart';
import 'package:gymapp_trainee/features/home/data/repositories/home_repository.dart';

/// Minimal fake datasource that records calls and returns canned responses —
/// lets us exercise HomeRepository without spinning up Dio.
class _FakeHomeRemoteDataSource implements HomeRemoteDataSource {
  WorkoutModel? workout;
  List<SessionModel> sessions = const [];
  ProgressDashboardModel? dashboard;
  WaterLogModel? water;

  int workoutCalls = 0;
  int sessionCalls = 0;

  @override
  Future<WorkoutModel?> getTodaysWorkout({
    required String userId,
    required String gymId,
  }) async {
    workoutCalls++;
    return workout;
  }

  @override
  Future<List<SessionModel>> getUpcomingSessions({
    required String userId,
    required String gymId,
  }) async {
    sessionCalls++;
    return sessions;
  }

  @override
  Future<ProgressDashboardModel> getProgressDashboard({
    required String userId,
    required String gymId,
  }) async =>
      dashboard!;

  @override
  Future<WaterLogModel> getTodaysWater({
    required String userId,
    required String gymId,
  }) async =>
      water!;
}

void main() {
  group('HomeRepository', () {
    late _FakeHomeRemoteDataSource fake;
    late HomeRepository repo;

    setUp(() {
      fake = _FakeHomeRemoteDataSource();
      repo = HomeRepository(remoteDataSource: fake);
    });

    test('returns null when no workout is assigned (remote mode off)',
        () async {
      fake.workout = null;
      expect(
        await repo.getTodaysWorkout(userId: 'u1', gymId: 'g1'),
        isNull,
      );
      expect(fake.workoutCalls, 1);
    });

    test('delegates getUpcomingSessions to datasource', () async {
      fake.sessions = [
        SessionModel(
          id: 's1',
          gymId: 'g1',
          trainerId: 't1',
          memberId: 'me',
          scheduledAt: DateTime(2026, 4, 20, 9),
          durationMinutes: 60,
          sessionType: 'PT',
          status: 'Scheduled',
          createdAt: DateTime(2026, 4, 10),
        ),
      ];
      final sessions =
          await repo.getUpcomingSessions(userId: 'u1', gymId: 'g1');
      expect(sessions, hasLength(1));
      expect(sessions.first.id, 's1');
      expect(fake.sessionCalls, 1);
    });
  });
}
