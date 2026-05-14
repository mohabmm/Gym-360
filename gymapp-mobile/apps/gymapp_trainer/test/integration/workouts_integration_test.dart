import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers WorkoutBuilderScreen, TemplateLibraryScreen, ExerciseManagerScreen,
/// ProgramBuilderScreen.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('WorkoutBuilderScreen', () {
    test('GET /training/workouts filtered by trainer+gym', () async {
      t.adapter.onGet(
        ApiEndpoints.workouts,
        respond: (_) => t.adapter.ok([Samples.workout()]),
      );

      final workouts = await t.apiClient.get<List<WorkoutModel>>(
        ApiEndpoints.workouts,
        queryParameters: {'trainerId': kTrainerId, 'gymId': kGymId},
        parser: (data) => (data as List<dynamic>)
            .map((e) => WorkoutModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.method, 'GET');
      expect(req.path, '/api/v1/training/workouts');
      expect(req.q('trainerId'), kTrainerId);
      expect(req.q('gymId'), kGymId);
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
      expect(body['trainerId'], kTrainerId);
      expect(body['gymId'], kGymId);
      expect(body['name'], 'Push Day A');
      expect(body['isTemplate'], false);
      expect((body['exercises'] as List).single['exerciseId'], 'ex-1');
      expect((body['exercises'] as List).single['reps'], '10'); // string!
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
      expect(req.method, 'PUT');
      expect(req.path, '/api/v1/training/workouts/workout-1');
      expect(req.bodyAsMap!.containsKey('trainerId'), false);
      expect(req.bodyAsMap!['name'], 'Push Day A v2');
    });
  });

  group('TemplateLibraryScreen', () {
    test('GET /training/templates filtered by gym', () async {
      t.adapter.onGet(
        ApiEndpoints.workoutTemplates,
        respond: (_) =>
            t.adapter.ok([Samples.workout(id: 'tpl-1')]),
      );

      final templates = await t.apiClient.get<List<WorkoutModel>>(
        ApiEndpoints.workoutTemplates,
        queryParameters: {'gymId': kGymId},
        parser: (data) => (data as List<dynamic>)
            .map((e) => WorkoutModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      expect(t.onlyRequest.q('gymId'), kGymId);
      expect(templates.single.id, 'tpl-1');
    });
  });

  group('ExerciseManagerScreen', () {
    test('GET /training/exercises returns full library', () async {
      t.adapter.onGet(
        ApiEndpoints.exercises,
        respond: (_) => t.adapter.ok([Samples.exercise()]),
      );

      final exercises = await t.apiClient.get<List<ExerciseModel>>(
        ApiEndpoints.exercises,
        parser: (data) => (data as List<dynamic>)
            .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      expect(t.onlyRequest.path, '/api/v1/training/exercises');
      expect(exercises.single.name, 'Bench Press');
    });

    test('GET /training/exercises/custom returns trainer-scoped exercises',
        () async {
      t.adapter.onGet(
        ApiEndpoints.customExercises,
        respond: (_) => t.adapter
            .ok([Samples.exercise(id: 'custom-1')..['isCustom'] = true]),
      );

      final customs = await t.apiClient.get<List<ExerciseModel>>(
        ApiEndpoints.customExercises,
        parser: (data) => (data as List<dynamic>)
            .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      expect(t.onlyRequest.path, '/api/v1/training/exercises/custom');
      expect(customs.single.id, 'custom-1');
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

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['scope'], 'custom');
      expect(body['gymId'], kGymId);
      expect(body['createdBy'], kTrainerId);
      expect(body['name'], 'Landmine Row');
    });
  });

  group('ProgramBuilderScreen', () {
    test('GET /training/programs filtered by trainer+gym', () async {
      t.adapter.onGet(
        ApiEndpoints.programs,
        respond: (_) => t.adapter.ok([Samples.program()]),
      );

      final programs = await t.apiClient.get<List<ProgramModel>>(
        ApiEndpoints.programs,
        queryParameters: {'trainerId': kTrainerId, 'gymId': kGymId},
        parser: (data) => (data as List<dynamic>)
            .map((e) => ProgramModel.fromJson(e as Map<String, dynamic>))
            .toList(),
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
