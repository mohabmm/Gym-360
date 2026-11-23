import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

/// A finished plan workout waiting on this phone to be uploaded. The [clientId] is made once,
/// when the workout is finished, so uploading it again can never count it twice.
class PendingPlanWorkout {
  final String clientId;
  /// Whose workout it is: a shared phone only uploads the signed-in trainee's own.
  final String userId;
  final String planId;
  final String dayKey;
  final DateTime finishedAt;
  final Map<String, dynamic> body;

  const PendingPlanWorkout({
    required this.clientId,
    required this.userId,
    required this.planId,
    required this.dayKey,
    required this.finishedAt,
    required this.body,
  });

  /// Kept as long as any offline write (founder decision: 7 days).
  bool isExpired(DateTime now) =>
      now.difference(finishedAt) > const Duration(days: AppConstants.offlineQueueRetentionDays);

  Map<String, dynamic> toJson() => {
        'clientId': clientId,
        'userId': userId,
        'planId': planId,
        'dayKey': dayKey,
        'finishedAt': finishedAt.toUtc().toIso8601String(),
        'body': body,
      };

  factory PendingPlanWorkout.fromJson(Map<String, dynamic> json) => PendingPlanWorkout(
        clientId: json['clientId'] as String,
        userId: json['userId'] as String? ?? '',
        planId: json['planId'] as String,
        dayKey: json['dayKey'] as String,
        finishedAt: DateTime.parse(json['finishedAt'] as String),
        body: jsonMap(json['body']),
      );
}

/// A plain `Map<String, dynamic>` all the way down (Hive hands maps back as `Map<dynamic, dynamic>`).
Map<String, dynamic> jsonMap(dynamic value) => {
      for (final e in Map<String, dynamic>.from(value as Map).entries) e.key: _jsonValue(e.value),
    };

dynamic _jsonValue(dynamic value) => switch (value) {
      Map() => jsonMap(value),
      List() => [for (final v in value) _jsonValue(v)],
      _ => value,
    };

/// Where pending workouts live until they are uploaded.
abstract class PlanWorkoutQueue {
  List<PendingPlanWorkout> all();
  Future<void> add(PendingPlanWorkout workout);
  Future<void> remove(String clientId);
}

/// Hive-backed queue, keyed by client id (so adding the same workout twice keeps one).
class HivePlanWorkoutQueue implements PlanWorkoutQueue {
  static const String boxName = 'plan_workout_queue_box';

  Box get _box => Hive.box(boxName);

  @override
  List<PendingPlanWorkout> all() => [
        for (final value in _box.values) PendingPlanWorkout.fromJson(jsonMap(value)),
      ]..sort((a, b) => a.finishedAt.compareTo(b.finishedAt));

  @override
  Future<void> add(PendingPlanWorkout workout) => _box.put(workout.clientId, workout.toJson());

  @override
  Future<void> remove(String clientId) => _box.delete(clientId);
}

/// In-memory queue for tests and for when the box is not open.
class MemoryPlanWorkoutQueue implements PlanWorkoutQueue {
  final Map<String, PendingPlanWorkout> _items = {};

  @override
  List<PendingPlanWorkout> all() => _items.values.toList()..sort((a, b) => a.finishedAt.compareTo(b.finishedAt));

  @override
  Future<void> add(PendingPlanWorkout workout) async => _items[workout.clientId] = workout;

  @override
  Future<void> remove(String clientId) async => _items.remove(clientId);
}
