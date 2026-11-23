import 'package:gymapp_core/gymapp_core.dart';

import 'plan_workout_queue.dart';

/// Uploads one pending workout; returns what the server did with it.
typedef PlanWorkoutUploader = Future<PlanWorkoutOutcome> Function(PendingPlanWorkout workout);

/// What one sync pass did.
class PlanSyncReport {
  final int uploaded;
  final int remaining;
  /// Uploads the server kept with a note (plan changed / replaced / already done that day).
  final List<PlanWorkoutOutcome> notes;
  /// Workouts the server refused for good (e.g. the plan is gone); they are dropped.
  final int rejected;

  const PlanSyncReport({this.uploaded = 0, this.remaining = 0, this.notes = const [], this.rejected = 0});
}

/// Sends pending plan workouts oldest first. Safe to run any time and as often as you like:
/// every upload is idempotent on the server. Stops at the first "no connection" and keeps the
/// rest for next time; drops a workout only when the server refuses it for good.
class PlanWorkoutSync {
  final PlanWorkoutQueue queue;
  final PlanWorkoutUploader upload;
  final DateTime Function() now;

  PlanWorkoutSync({required this.queue, required this.upload, DateTime Function()? now}) : now = now ?? DateTime.now;

  Future<PlanSyncReport>? _inFlight;

  /// Uploads [userId]'s pending workouts. Passes never overlap: a call made during a pass
  /// waits for it and then runs its own, so a workout added meanwhile is not missed.
  Future<PlanSyncReport> flush(String userId) async {
    while (_inFlight != null) {
      await _inFlight;
    }
    final pass = _flush(userId);
    _inFlight = pass;
    try {
      return await pass;
    } finally {
      _inFlight = null;
    }
  }

  List<PendingPlanWorkout> _mine(String userId) => queue.all().where((w) => w.userId == userId).toList();

  Future<PlanSyncReport> _flush(String userId) async {
    var uploaded = 0;
    var rejected = 0;
    final notes = <PlanWorkoutOutcome>[];
    for (final workout in _mine(userId)) {
      if (workout.isExpired(now())) {
        await queue.remove(workout.clientId);
        rejected++;
        continue;
      }
      final step = await _send(workout);
      if (step == null) break;
      await queue.remove(workout.clientId);
      if (step.rejected) {
        rejected++;
      } else {
        uploaded++;
        if (step.outcome.isNote) notes.add(step.outcome);
      }
    }
    return PlanSyncReport(uploaded: uploaded, remaining: _mine(userId).length, notes: notes, rejected: rejected);
  }

  /// null = try again later (offline, server busy, signed out, or plans paused for the gym).
  Future<({PlanWorkoutOutcome outcome, bool rejected})?> _send(PendingPlanWorkout workout) async {
    try {
      return (outcome: await upload(workout), rejected: false);
    } on ApiException catch (e) {
      if (isRetryable(e)) return null;
      return (outcome: PlanWorkoutOutcome.created, rejected: true);
    }
  }

  static bool isRetryable(ApiException e) {
    final status = e.statusCode;
    if (status == null || e is FeatureDisabledException) return true;
    return status == 401 || status == 408 || status == 429 || status >= 500;
  }
}

extension on PlanWorkoutOutcome {
  bool get isNote =>
      this == PlanWorkoutOutcome.planChanged || this == PlanWorkoutOutcome.planReplaced || this == PlanWorkoutOutcome.duplicate;
}
