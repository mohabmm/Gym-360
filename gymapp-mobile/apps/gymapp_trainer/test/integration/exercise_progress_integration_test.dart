import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_draft.dart';

import '../exercise_progress_fixtures.dart';
import 'helpers/test_api_client.dart';

/// pt.exercise_progress: the coach's two calls — read the client's board, save a session's exercises.
void main() {
  late TestApiClient t;

  setUp(() => t = buildTestApiClient());

  test('GET /scheduling/clients/{id}/exercise-progress parses the board', () async {
    t.adapter.onGet(
      ApiEndpoints.clientExerciseProgress(const ClientId('c1')),
      respond: (_) => t.adapter.ok(trialBoardJson()),
    );

    final board = await t.apiClient.get<ExerciseProgressModel>(
      ApiEndpoints.clientExerciseProgress(const ClientId('c1')),
      parser: (d) => ExerciseProgressModel.fromJson(Map<String, dynamic>.from(d as Map)),
    );

    expect(t.onlyRequest.path, '/api/v1/scheduling/clients/c1/exercise-progress');
    expect(board.days.first.label, 'Push Day');
    expect(board.exercises.first.nextTarget?.weight, 65);
  });

  test('POST /scheduling/sessions/{id}/exercise-logs sends the day label, numbers and target', () async {
    t.adapter.onPost(
      ApiEndpoints.sessionExerciseLogs(const SessionId('s1')),
      respond: (_) => t.adapter.created(const <Object>[]),
    );
    final draft = SessionExercisesDraft()..applyRepeat(trialBoard(), 'Push Day');
    draft.rows.single.targetWeight = 67.5;

    await t.apiClient.post<Object?>(
      ApiEndpoints.sessionExerciseLogs(const SessionId('s1')),
      data: draft.toJson(),
      parser: (d) => d,
    );

    final req = t.onlyRequest;
    expect(req.method, 'POST');
    expect(req.path, '/api/v1/scheduling/sessions/s1/exercise-logs');
    final body = req.body as Map;
    expect(body['dayLabel'], 'Push Day');
    final item = (body['items'] as List).single as Map;
    expect(item['exerciseName'], 'Bench press');
    expect(item['weight'], 65);
    expect((item['target'] as Map)['weight'], 67.5);
  });
}
