import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/recorded_request.dart';
import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers WorkoutBuilderScreen, TemplateLibraryScreen, ExerciseManagerScreen,
/// ProgramBuilderScreen.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  /// Stubs GET [path] with [payload] and fetches it as a typed list.
  Future<List<T>> fetchList<T>(
    String path,
    List<Map<String, dynamic>> payload,
    T Function(Map<String, dynamic> json) fromJson, {
    Map<String, dynamic>? query,
  }) {
    t.adapter.onGet(path, respond: (_) => t.adapter.ok(payload));
    return t.apiClient.get<List<T>>(
      path,
      queryParameters: query,
      parser: (data) => (data as List<dynamic>)
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Matches a recorded request by method, path and (optionally) query values.
  Matcher isRequest(
    String method,
    String path, {
    Map<String, String> query = const {},
  }) {
    var matcher = isA<RecordedRequest>()
        .having((r) => r.method, 'method', method)
        .having((r) => r.path, 'path', path);
    query.forEach((key, value) {
      matcher = matcher.having((r) => r.q(key), 'query[$key]', value);
    });
    return matcher;
  }

  group('WorkoutBuilderScreen', () {
    test('GET /training/workouts filtered by trainer+gym', () async {
      final workouts = await fetchList(
        ApiEndpoints.workouts,
        [Samples.workout()],
        WorkoutModel.fromJson,
        query: {'trainerId': kTrainerId, 'gymId': kGymId},
      );

      expect(
        t.onlyRequest,
        isRequest('GET', '/api/v1/training/workouts',
            query: {'trainerId': kTrainerId, 'gymId': kGymId}),
      );
      expect(workouts.single.exercises, hasLength(1));
    });

    test('GET /training/workouts/{id} returns single workout', () async {
      t.adapter.onGet(
        ApiEndpoints.workoutById('workout-1'),
        respond: (_) => t.adapter.ok(Samples.workout()),
      );

      final w = await t.apiClient.get<WorkoutModel>(
        ApiEndpoints.workoutById('workout-1'),
        parser: (d) => WorkoutModel.fromJson(d as Map<String, dynamic>),
      );

      expect(t.onlyRequest.path, '/api/v1/training/workouts/workout-1');
      expect(w.id, 'workout-1');
      expect(w.name, 'Push Day A');
    });

    test('POST /training/workouts creates workout with full payload', () async {
      t.adapter.onPost(
        ApiEndpoints.workouts,
        respond: (_) => t.adapter.created(Samples.workout()),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.workouts,
        data: {
          'trainerId': kTrainerId,
          'gymId': kGymId,
          'name': 'Push Day A',
          'nameAr': 'يوم الدفع أ',
          'description': null,
          'exercises': [
            {
              'exerciseId': 'ex-1',
              'sets': 4,
              'reps': '10',
              'restSeconds': 90,
            },
          ],
          'isTemplate': false,
        },
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(
        body,
        allOf(
          containsPair('trainerId', kTrainerId),
          containsPair('gymId', kGymId),
          containsPair('name', 'Push Day A'),
          containsPair('isTemplate', false),
        ),
      );
      final exercise = (body['exercises'] as List).single as Map;
      expect(exercise['exerciseId'], 'ex-1');
      expect(exercise['reps'], '10'); // string!
    });

    test('PUT /training/workouts/{id} updates workout without trainer/gym',
        () async {
      t.adapter.onPut(
        ApiEndpoints.workoutById('workout-1'),
        respond: (_) => t.adapter.noContent(),
      );

      await t.apiClient.putVoid(
        ApiEndpoints.workoutById('workout-1'),
        data: {
          'name': 'Push Day A v2',
          'description': 'Updated',
          'exercises': <Map<String, dynamic>>[],
        },
      );

      final req = t.onlyRequest;
      expect(req, isRequest('PUT', '/api/v1/training/workouts/workout-1'));
      expect(req.bodyAsMap!.containsKey('trainerId'), false);
      expect(req.bodyAsMap!['name'], 'Push Day A v2');
    });
  });

  group('TemplateLibraryScreen', () {
    test('GET /training/templates filtered by gym', () async {
      final templates = await fetchList(
        ApiEndpoints.workoutTemplates,
        [Samples.workout(id: 'tpl-1')],
        WorkoutModel.fromJson,
        query: {'gymId': kGymId},
      );

      expect(t.onlyRequest.q('gymId'), kGymId);
      expect(templates.single.id, 'tpl-1');
    });
  });

  group('ExerciseManagerScreen', () {
    test('GET /training/exercises returns full library', () async {
      final exercises = await fetchList(
        ApiEndpoints.exercises,
        [Samples.exercise()],
        ExerciseModel.fromJson,
      );

      expect(t.onlyRequest.path, '/api/v1/training/exercises');
      expect(exercises.single.name, 'Bench Press');
    });

    test('GET /training/exercises/custom returns trainer-scoped exercises',
        () async {
      final customs = await fetchList(
        ApiEndpoints.customExercises,
        [Samples.exercise(id: 'custom-1')..['scope'] = 'Custom'],
        ExerciseModel.fromJson,
      );

      expect(t.onlyRequest.path, '/api/v1/training/exercises/custom');
      expect(customs.single.id, 'custom-1');
      expect(customs.single.isCustom, true);
    });

    test('POST /training/exercises creates custom exercise', () async {
      t.adapter.onPost(
        ApiEndpoints.exercises,
        respond: (_) => t.adapter.created(Samples.exercise()),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.exercises,
        data: {
          'name': 'Landmine Row',
          'nameAr': 'سحب لاند ماين',
          'muscleGroups': <String>[],
          'equipment': 'none',
          'category': 'other',
          'scope': 'custom',
          'gymId': kGymId,
          'createdBy': kTrainerId,
        },
      );

      expect(
        t.onlyRequest.bodyAsMap,
        allOf(
          containsPair('scope', 'custom'),
          containsPair('gymId', kGymId),
          containsPair('createdBy', kTrainerId),
          containsPair('name', 'Landmine Row'),
        ),
      );
    });
  });

  group('ProgramBuilderScreen', () {
    test('GET /training/programs filtered by trainer+gym', () async {
      final programs = await fetchList(
        ApiEndpoints.programs,
        [Samples.program()],
        ProgramModel.fromJson,
        query: {'trainerId': kTrainerId, 'gymId': kGymId},
      );

      final req = t.onlyRequest;
      expect(req.q('trainerId'), kTrainerId);
      expect(req.q('gymId'), kGymId);
      expect(programs.single.durationWeeks, 8);
    });

    test('POST /training/programs sends nested weeks+dayWorkouts', () async {
      t.adapter.onPost(
        ApiEndpoints.programs,
        respond: (_) => t.adapter.created(Samples.program()),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.programs,
        data: {
          'trainerId': kTrainerId,
          'gymId': kGymId,
          'name': '8-Week Strength',
          'nameAr': 'قوة 8 اسابيع',
          'description': null,
          'durationWeeks': 8,
          'weeks': [
            {
              'weekNumber': 1,
              'dayWorkouts': [
                {'dayOfWeek': 'Monday', 'workoutId': 'workout-1'},
              ],
            },
          ],
          'isTemplate': false,
        },
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['durationWeeks'], 8);
      expect(body['weeks'], isA<List>());
      final firstWeek = (body['weeks'] as List).single as Map<String, dynamic>;
      expect(firstWeek['weekNumber'], 1);
      expect((firstWeek['dayWorkouts'] as List).single['dayOfWeek'], 'Monday');
    });
  });
}
