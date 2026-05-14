import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/gamification/data/datasources/gamification_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Leaderboard screen, trainer public profile, rate-trainer flow, opt-out.
void main() {
  late TestApiClient t;
  late GamificationRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = GamificationRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET leaderboard by type parses entries + my position', () async {
    t.adapter.onGet(
      ApiEndpoints.leaderboardByType('workouts_per_month'),
      respond: (_) => t.adapter.ok(Samples.leaderboard()),
    );
    final lb = await ds.getLeaderboard('workouts_per_month', gymId: 'gym-1');
    expect(lb.entries, hasLength(2));
    expect(lb.myPosition!.position, 2);
    expect(t.onlyRequest.q('gymId'), 'gym-1');
  });

  test('GET trainer public profile parses rating + badges', () async {
    t.adapter.onGet(
      ApiEndpoints.trainerPublicProfile('trainer-1'),
      respond: (_) => t.adapter.ok(Samples.trainerPublicProfile()),
    );
    final p = await ds.getTrainerPublicProfile('trainer-1', gymId: 'gym-1');
    expect(p.averageRating, 4.7);
    expect(p.totalReviews, 31);
  });

  test('POST trainer-ratings returns rating id', () async {
    t.adapter.onPost(
      ApiEndpoints.trainerRatings,
      respond: (_) => t.adapter.created('rating-1'),
    );
    final id = await ds.rateTrainer(
      const RateTrainerRequest(
        gymId: 'gym-1',
        trainerId: 'trainer-1',
        clientId: 'user-1',
        sessionId: 'session-1',
        stars: 5,
        comment: 'Great coach',
      ),
    );
    expect(id, 'rating-1');
    expect(t.onlyRequest.bodyAsMap!['stars'], 5);
  });

  test('PUT leaderboards/opt-out sends userId + gymId', () async {
    t.adapter.onPut(
      ApiEndpoints.leaderboardOptOut,
      respond: (_) => t.adapter.noContent(),
    );
    await ds.toggleLeaderboardOptOut(userId: 'user-1', gymId: 'gym-1');
    expect(t.onlyRequest.bodyAsMap, {'userId': 'user-1', 'gymId': 'gym-1'});
  });
}
