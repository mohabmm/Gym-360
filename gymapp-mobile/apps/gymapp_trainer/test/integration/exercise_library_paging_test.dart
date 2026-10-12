import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/features/workouts/presentation/providers/workout_providers.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// The exercise picker and library search client-side, so
/// [exerciseLibraryProvider] must load every page. It used to request only
/// page 1 (100 rows) — with a 242-exercise library everything alphabetically
/// past the first 100 could not be found or added to a workout.
void main() {
  late TestApiClient t;
  late ProviderContainer container;

  setUp(() {
    t = buildTestApiClient();
    container = ProviderContainer(
      overrides: [apiClientProvider.overrideWithValue(t.apiClient)],
    );
  });

  tearDown(() => container.dispose());

  Map<String, dynamic> page(int page, int count, int totalPages) => {
        'items': [
          for (var i = 0; i < count; i++)
            Samples.exercise(id: 'ex-p$page-$i'),
        ],
        'page': page,
        'pageSize': 100,
        'totalCount': 242,
        'totalPages': totalPages,
      };

  test('loads every page of the library, not just the first', () async {
    t.adapter.onGet(
      ApiEndpoints.exercises,
      respond: (req) {
        final p = int.parse(req.q('page')!);
        return t.adapter.ok(page(p, p < 3 ? 100 : 42, 3));
      },
    );

    final exercises = await container.read(exerciseLibraryProvider.future);

    expect(exercises, hasLength(242));
    expect(exercises.last.id, 'ex-p3-41');
    expect(
      t.adapter.recorded.map((r) => r.q('page')).toList(),
      ['1', '2', '3'],
    );
    expect(t.adapter.recorded.every((r) => r.q('pageSize') == '100'), isTrue);
  });

  test('single-page library issues exactly one request', () async {
    t.adapter.onGet(
      ApiEndpoints.exercises,
      respond: (_) => t.adapter.ok(page(1, 7, 1)),
    );

    final exercises = await container.read(exerciseLibraryProvider.future);

    expect(exercises, hasLength(7));
    expect(t.adapter.recorded, hasLength(1));
  });

  test('stops on an empty page even if totalPages overstates', () async {
    t.adapter.onGet(
      ApiEndpoints.exercises,
      respond: (req) {
        final p = int.parse(req.q('page')!);
        return t.adapter.ok(page(p, p == 1 ? 100 : 0, 50));
      },
    );

    final exercises = await container.read(exerciseLibraryProvider.future);

    expect(exercises, hasLength(100));
    expect(t.adapter.recorded, hasLength(2));
  });

  test('accepts a bare list response (legacy shape)', () async {
    t.adapter.onGet(
      ApiEndpoints.exercises,
      respond: (_) => t.adapter.ok([Samples.exercise()]),
    );

    final exercises = await container.read(exerciseLibraryProvider.future);

    expect(exercises.single.id, 'ex-1');
    expect(t.adapter.recorded, hasLength(1));
  });
}
