import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

import '../../../core/providers.dart';
import 'plan_providers.dart';
import 'plan_workout_queue.dart';
import 'plan_workout_sync.dart';

/// The last challenge status the server sent, per user, so the card still shows offline.
abstract class ChallengeCache {
  Map<String, dynamic>? read(String userId);
  Future<void> write(String userId, Map<String, dynamic> json);
}

class HiveChallengeCache implements ChallengeCache {
  static String _key(String userId) => 'my_first_month_challenge:$userId';

  Box get _box => Hive.box(StorageKeys.workoutCacheBox);

  @override
  Map<String, dynamic>? read(String userId) {
    final value = _box.get(_key(userId));
    return value is Map ? jsonMap(value) : null;
  }

  @override
  Future<void> write(String userId, Map<String, dynamic> json) => _box.put(_key(userId), json);
}

class MemoryChallengeCache implements ChallengeCache {
  final Map<String, Map<String, dynamic>> _items = {};

  @override
  Map<String, dynamic>? read(String userId) => _items[userId];

  @override
  Future<void> write(String userId, Map<String, dynamic> json) async => _items[userId] = json;
}

final challengeCacheProvider = Provider<ChallengeCache>((ref) =>
    Hive.isBoxOpen(StorageKeys.workoutCacheBox) ? HiveChallengeCache() : MemoryChallengeCache());

/// mem.first_month_challenge: "8 / 12" and the reward. Workouts saved offline count straight
/// away; the server count wins once they upload. null = the challenge is off for this gym.
final myChallengeProvider = FutureProvider.autoDispose<ChallengeStatusModel?>((ref) async {
  final userId = ref.watch(authProvider).valueOrNull?.user?.id ?? '';
  final cache = ref.watch(challengeCacheProvider);
  final pending = ref.watch(planWorkoutQueueProvider).all().where((w) => w.userId == userId).length;
  try {
    final json = await ref.read(apiClientProvider).get<Map<String, dynamic>>(
          ApiEndpoints.myChallenge,
          parser: (data) => Map<String, dynamic>.from(data as Map),
        );
    await cache.write(userId, json);
    return ChallengeStatusModel.fromJson(json).withPending(pending);
  } on FeatureDisabledException {
    return null;
  } on ApiException catch (e) {
    final cached = PlanWorkoutSync.isRetryable(e) ? cache.read(userId) : null;
    if (cached == null) rethrow;
    return ChallengeStatusModel.fromJson(cached).withPending(pending);
  }
});
