import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/progress/data/datasources/progress_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Progress screen — dashboard, body metrics, photos.
void main() {
  late TestApiClient t;
  late ProgressRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = ProgressRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET /progress/dashboard parses streak + latestMetrics', () async {
    t.adapter.onGet(
      ApiEndpoints.progressDashboard,
      respond: (_) => t.adapter.ok(Samples.progressDashboard()),
    );
    final d = await ds.getDashboard(userId: 'user-1', gymId: 'gym-1');
    expect(d.currentWorkoutStreak, 4);
    expect(d.latestMetrics?.weight, 72.5);
  });

  test('GET /progress/body-metrics forwards optional filters', () async {
    t.adapter.onGet(
      ApiEndpoints.bodyMetrics,
      respond: (_) => t.adapter.ok([Samples.bodyMetric()]),
    );
    await ds.getBodyMetrics(
      userId: 'user-1',
      gymId: 'gym-1',
      metricType: 'Weight',
      from: DateTime.utc(2026, 1, 1),
      to: DateTime.utc(2026, 4, 16),
    );
    final q = t.onlyRequest.query;
    expect(q['metricType']!.first, 'Weight');
    expect(q['from']!.first, '2026-01-01');
    expect(q['to']!.first, '2026-04-16');
  });

  test('POST /progress/body-metrics returns new metric id', () async {
    t.adapter.onPost(
      ApiEndpoints.bodyMetrics,
      respond: (_) => t.adapter.created('bm-new'),
    );
    final id = await ds.logBodyMetric({
      'userId': 'user-1',
      'gymId': 'gym-1',
      'metricType': 'Weight',
      'value': 72.3,
      'unit': 'kg',
      'recordedAt': '2026-04-16T08:00:00.000Z',
    });
    expect(id, 'bm-new');
    expect(t.onlyRequest.bodyAsMap!['value'], 72.3);
  });

  test('GET /progress/photos returns photo sets', () async {
    t.adapter.onGet(
      ApiEndpoints.progressPhotos,
      respond: (_) => t.adapter.ok([Samples.progressPhotoSet()]),
    );
    final sets =
        await ds.getProgressPhotos(userId: 'user-1', gymId: 'gym-1');
    expect(sets, hasLength(1));
    expect(sets.first.front!.imageUrl, contains('cdn.test'));
  });

  test('POST /progress/photos — storage limit (403) surfaces as typed error',
      () async {
    t.adapter.onPost(
      ApiEndpoints.progressPhotos,
      respond: (_) => t.adapter.rawResponse(403, {
        'data': null,
        'errors': [
          {'code': 'STORAGE_LIMIT', 'message': 'Photo limit reached'},
        ],
        'errorCode': 'STORAGE_LIMIT',
        'currentCount': 20,
        'maxCount': 20,
        'resourceType': 'photos',
      }),
    );
    await expectLater(
      ds.uploadProgressPhoto({
        'userId': 'user-1',
        'gymId': 'gym-1',
        'type': 'Front',
        'imageUrl': 'https://cdn.test/new.jpg',
      }),
      throwsA(isA<StorageLimitException>()
          .having((e) => e.maxCount, 'maxCount', 20)
          .having((e) => e.resourceType, 'resourceType', 'photos')),
    );
  });

  test('DELETE /progress/photos/{id} carries userId query', () async {
    t.adapter.onDelete(
      ApiEndpoints.progressPhotoById('ph-1'),
      respond: (_) => t.adapter.noContent(),
    );
    await ds.deleteProgressPhoto('ph-1', userId: 'user-1');
    expect(t.onlyRequest.q('userId'), 'user-1');
  });
}
