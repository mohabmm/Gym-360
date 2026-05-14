import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers MyRatingsDashboardScreen.
/// Trainers see the aggregated public profile (rating + anonymized
/// testimonials). They do NOT see individual rating rows.
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  test('GET /gamification/trainers/{id}/public-profile returns summary',
      () async {
    t.adapter.onGet(
      ApiEndpoints.trainerPublicProfile(kTrainerId),
      respond: (_) => t.adapter.ok(Samples.trainerPublicProfile()),
    );

    final profile = await t.apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.trainerPublicProfile(kTrainerId),
      queryParameters: {'gymId': kGymId},
      parser: (d) => d as Map<String, dynamic>,
    );

    final req = t.onlyRequest;
    expect(req.path, '/api/v1/gamification/trainers/$kTrainerId/public-profile');
    expect(req.q('gymId'), kGymId);
    expect((profile['averageRating'] as num).toDouble(), 4.7);
    expect(profile['totalReviews'], 31);
    expect(profile['testimonials'], isA<List>());
  });

  test('testimonials payload is a list of anonymized entries', () async {
    t.adapter.onGet(
      ApiEndpoints.trainerPublicProfile(kTrainerId),
      respond: (_) => t.adapter.ok({
        ...Samples.trainerPublicProfile(),
        'testimonials': [
          {'memberInitials': 'M.A.', 'comment': 'Great!', 'rating': 5},
          {'memberInitials': 'S.K.', 'comment': 'Very motivating', 'rating': 4},
        ],
      }),
    );

    final profile = await t.apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.trainerPublicProfile(kTrainerId),
      queryParameters: {'gymId': kGymId},
      parser: (d) => d as Map<String, dynamic>,
    );

    final testimonials = (profile['testimonials'] as List).cast<Map>();
    expect(testimonials, hasLength(2));
    expect(testimonials.first['memberInitials'], 'M.A.');
    // PDPL: individual member names MUST NOT be exposed to trainers.
    for (final tst in testimonials) {
      expect(tst.containsKey('memberName'), isFalse);
      expect(tst.containsKey('memberId'), isFalse);
    }
  });

  test('404 on public profile falls back gracefully', () async {
    t.adapter.onGet(
      ApiEndpoints.trainerPublicProfile(kTrainerId),
      respond: (_) => t.adapter.error(404, code: 'NOT_FOUND'),
    );

    await expectLater(
      t.apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.trainerPublicProfile(kTrainerId),
        queryParameters: {'gymId': kGymId},
        parser: (d) => d as Map<String, dynamic>,
      ),
      throwsA(isA<NotFoundException>()),
    );
  });
}
