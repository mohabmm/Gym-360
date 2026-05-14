import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/workouts/data/datasources/workouts_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Workouts screen — list/detail workouts, templates, logs, exercise DB,
/// workout assignments.
void main() {
  late TestApiClient t;
  late WorkoutsRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = WorkoutsRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET /training/workouts returns list', () async {
    t.adapter.onGet(
      ApiEndpoints.workouts,
      respond: (_) => t.adapter.ok([Samples.workout()]),
    );
    final list = await ds.getWorkouts();
    expect(list.first.name, 'Push Day A');
    expect(t.onlyRequest.header('authorization'), 'Bearer test-jwt');
    expect(t.onlyRequest.header('accept-language'), 'ar');
  });

  test('GET /training/workouts/{id}', () async {
    t.adapter.onGet(
      ApiEndpoints.workoutById('workout-1'),
      respond: (_) => t.adapter.ok(Samples.workout()),
    );
    final w = await ds.getWorkoutById('workout-1');
    expect(w.trainerName, 'Coach Omar');
  });

  test('GET /training/templates', () async {
    t.adapter.onGet(
      ApiEndpoints.workoutTemplates,
      respond: (_) => t.adapter.ok([Samples.workout(id: 'tpl-1')]),
    );
    final list = await ds.getWorkoutTemplates();
    expect(list.first.id, 'tpl-1');
  });

  test('GET /training/workout-logs', () async {
    t.adapter.onGet(
      ApiEndpoints.workoutLogs,
      respond: (_) => t.adapter.ok([Samples.workoutLog()]),
    );
    final logs = await ds.getWorkoutLogs();
    expect(logs.first.sets, hasLength(1));
  });

  test('POST /training/workout-logs forwards set entries', () async {
    t.adapter.onPost(
      ApiEndpoints.workoutLogs,
      respond: (_) => t.adapter.created(Samples.workoutLog(id: 'log-new')),
    );
    final log = await ds.logWorkout({
      'workoutId': 'workout-1',
      'date': '2026-04-16',
      'startedAt': '2026-04-16T09:00:00.000Z',
      'completedAt': '2026-04-16T09:45:00.000Z',
      'durationMinutes': 45,
      'sets': [
        {
          'exerciseId': 'ex-1',
          'setNumber': 1,
          'reps': 10,
          'weight': 60.0,
          'weightUnit': 'kg',
          'isCompleted': true,
        },
      ],
      'perceivedExertion': 7,
    });
    expect(log.id, 'log-new');
    final body = t.onlyRequest.bodyAsMap!;
    expect(body['date'], '2026-04-16');
    expect(body['startedAt'], '2026-04-16T09:00:00.000Z');
    final sets = body['sets'] as List;
    expect(sets, hasLength(1));
    final set = sets.first as Map<String, dynamic>;
    expect(set['exerciseId'], 'ex-1');
    expect(set['setNumber'], 1);
    expect(set['reps'], 10);
    expect(set['weightUnit'], 'kg');
    expect(set['isCompleted'], true);
  });

  test('GET /training/exercises forwards search + muscleGroup', () async {
    t.adapter.onGet(
      ApiEndpoints.exercises,
      respond: (_) => t.adapter.ok([Samples.exercise()]),
    );
    await ds.getExercises(query: 'bench', muscleGroup: 'Chest');
    expect(t.onlyRequest.q('search'), 'bench');
    expect(t.onlyRequest.q('muscleGroup'), 'Chest');
  });

  test('GET /training/workout-assignments', () async {
    t.adapter.onGet(
      ApiEndpoints.workoutAssignments,
      respond: (_) => t.adapter.ok([
        {
          'id': 'wa-1',
          'workoutId': 'workout-1',
          'clientId': 'user-1',
          'trainerId': 'trainer-1',
          'gymId': 'gym-1',
          'assignmentType': 'Workout',
          'startDate': '2026-04-10',
          'endDate': '2026-04-16',
          'status': 'Active',
          'isRemoteMode': false,
        },
      ]),
    );
    final list = await ds.getWorkoutAssignments();
    expect(list.first.clientId, 'user-1');
  });
}
