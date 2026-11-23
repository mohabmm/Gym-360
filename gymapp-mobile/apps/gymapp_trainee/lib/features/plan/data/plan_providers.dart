import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

import '../../../core/providers.dart';
import 'my_plan_view.dart';
import 'plan_workout_queue.dart';
import 'plan_workout_sync.dart';

final planWorkoutQueueProvider = Provider<PlanWorkoutQueue>((ref) =>
    Hive.isBoxOpen(HivePlanWorkoutQueue.boxName) ? HivePlanWorkoutQueue() : MemoryPlanWorkoutQueue());

final planWorkoutSyncProvider = Provider<PlanWorkoutSync>((ref) {
  final api = ref.watch(apiClientProvider);
  return PlanWorkoutSync(
    queue: ref.watch(planWorkoutQueueProvider),
    upload: (w) => api.put<PlanWorkoutOutcome>(
      ApiEndpoints.myPlanWorkout(w.clientId),
      data: w.body,
      parser: (data) => PlanWorkoutOutcome.parse((data as Map?)?['outcome'] as String?),
    ),
  );
});

/// The last plan the server sent, per user, so the card and the workout open offline.
abstract class MyPlanCache {
  Map<String, dynamic>? read(String userId);
  Future<void> write(String userId, Map<String, dynamic>? json);
}

class HiveMyPlanCache implements MyPlanCache {
  static String _key(String userId) => 'my_training_plan:$userId';

  Box get _box => Hive.box(StorageKeys.workoutCacheBox);

  @override
  Map<String, dynamic>? read(String userId) {
    final value = _box.get(_key(userId));
    return value is Map ? jsonMap(value) : null;
  }

  @override
  Future<void> write(String userId, Map<String, dynamic>? json) =>
      json == null ? _box.delete(_key(userId)) : _box.put(_key(userId), json);
}

class MemoryMyPlanCache implements MyPlanCache {
  final Map<String, Map<String, dynamic>> _items = {};

  @override
  Map<String, dynamic>? read(String userId) => _items[userId];

  @override
  Future<void> write(String userId, Map<String, dynamic>? json) async =>
      json == null ? _items.remove(userId) : _items[userId] = json;
}

final myPlanCacheProvider = Provider<MyPlanCache>((ref) =>
    Hive.isBoxOpen(StorageKeys.workoutCacheBox) ? HiveMyPlanCache() : MemoryMyPlanCache());

String _userId(Ref ref) => ref.watch(authProvider).valueOrNull?.user?.id ?? '';

/// pt.training_plan: uploads anything pending, then the active plan with today's workout.
/// Offline it falls back to the cached plan. null = the coach has not given a plan.
final myPlanProvider = FutureProvider.autoDispose<MyPlanView?>((ref) async {
  final userId = _userId(ref);
  final queue = ref.watch(planWorkoutQueueProvider);
  final cache = ref.watch(myPlanCacheProvider);
  await ref.read(planWorkoutSyncProvider).flush(userId);

  final (json, fromCache) = await _loadPlan(ref, cache, userId);
  if (json == null) return null;
  return MyPlanView.build(MyPlanModel.fromJson(json), queue.all(), fromCache: fromCache);
});

Future<(Map<String, dynamic>?, bool)> _loadPlan(Ref ref, MyPlanCache cache, String userId) async {
  try {
    final json = await ref.read(apiClientProvider).get<Map<String, dynamic>?>(
          ApiEndpoints.myPlan,
          parser: (data) => data is Map ? Map<String, dynamic>.from(data) : null,
        );
    await cache.write(userId, json);
    return (json, false);
  } on ApiException catch (e) {
    final cached = _isOffline(e) ? cache.read(userId) : null;
    if (cached == null) rethrow;
    return (cached, true);
  }
}

/// No connection or a server hiccup — not "plans are off" or "not allowed".
bool _isOffline(ApiException e) => e is! FeatureDisabledException && PlanWorkoutSync.isRetryable(e);

final myPlanProgressProvider = FutureProvider.autoDispose<PlanProgressModel>((ref) async {
  await ref.read(planWorkoutSyncProvider).flush(_userId(ref));
  return ref.watch(apiClientProvider).get<PlanProgressModel>(
        ApiEndpoints.myPlanProgress,
        parser: (data) => PlanProgressModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});

/// What happened when the trainee pressed "Finish".
class FinishResult {
  final bool uploaded;
  final PlanWorkoutOutcome? note;

  const FinishResult({required this.uploaded, this.note});
}

/// Saves the finished workout on the phone first (so nothing is lost in a basement gym),
/// then tries to upload it straight away.
class PlanWorkoutRecorder {
  final Ref _ref;

  PlanWorkoutRecorder(this._ref);

  Future<FinishResult> finish(PendingPlanWorkout workout) async {
    await _ref.read(planWorkoutQueueProvider).add(workout);
    final report = await _ref.read(planWorkoutSyncProvider).flush(workout.userId);
    final stillPending = _ref.read(planWorkoutQueueProvider).all().any((w) => w.clientId == workout.clientId);
    _ref.invalidate(myPlanProvider);
    _ref.invalidate(myPlanProgressProvider);
    return FinishResult(uploaded: !stillPending, note: report.notes.lastOrNull);
  }
}

final planWorkoutRecorderProvider = Provider<PlanWorkoutRecorder>(PlanWorkoutRecorder.new);
