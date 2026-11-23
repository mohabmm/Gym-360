import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/plan/data/plan_workout_queue.dart';
import 'package:gymapp_trainee/features/plan/data/plan_workout_sync.dart';

PendingPlanWorkout _pending(String id, {String user = 'user-1', DateTime? at}) => PendingPlanWorkout(
      clientId: id,
      userId: user,
      planId: 'plan-1',
      dayKey: 'w1a',
      finishedAt: at ?? DateTime(2026, 11, 23, 18),
      body: {'planId': 'plan-1'},
    );

const _offline = ApiException(message: 'no network', errorCode: 'NETWORK_ERROR');

void main() {
  final now = DateTime(2026, 11, 24);

  test('offline: nothing is lost, and the next pass uploads every workout once, oldest first', () async {
    final queue = MemoryPlanWorkoutQueue();
    await queue.add(_pending('b', at: DateTime(2026, 11, 23, 19)));
    await queue.add(_pending('a', at: DateTime(2026, 11, 23, 18)));
    var online = false;
    final sent = <String>[];
    final sync = PlanWorkoutSync(
      queue: queue,
      now: () => now,
      upload: (w) async {
        if (!online) throw _offline;
        sent.add(w.clientId);
        return PlanWorkoutOutcome.created;
      },
    );

    final offline = await sync.flush('user-1');
    expect(offline.uploaded, 0);
    expect(offline.remaining, 2);

    online = true;
    final back = await sync.flush('user-1');
    expect(back.uploaded, 2);
    expect(sent, ['a', 'b']);
    expect((await sync.flush('user-1')).uploaded, 0, reason: 'a second pass sends nothing again');
  });

  test('the server answers for conflicts: notes are reported, and a workout refused for good is dropped', () async {
    final queue = MemoryPlanWorkoutQueue();
    await queue.add(_pending('changed', at: DateTime(2026, 11, 23, 10)));
    await queue.add(_pending('gone', at: DateTime(2026, 11, 23, 11)));
    await queue.add(_pending('retry', at: DateTime(2026, 11, 23, 12)));
    final sync = PlanWorkoutSync(
      queue: queue,
      now: () => now,
      upload: (w) async => switch (w.clientId) {
        'changed' => PlanWorkoutOutcome.planChanged,
        'gone' => throw const NotFoundException(),
        _ => PlanWorkoutOutcome.alreadySynced,
      },
    );

    final report = await sync.flush('user-1');

    expect(report.notes, [PlanWorkoutOutcome.planChanged]);
    expect(report.rejected, 1);
    expect(report.uploaded, 2);
    expect(queue.all(), isEmpty);
  });

  test('plans paused for the gym, a busy server or an expired login keep the workout for later', () async {
    for (final error in [
      const FeatureDisabledException(featureKey: 'pt.training_plan'),
      const ApiException(message: 'busy', statusCode: 503),
      const UnauthorizedException(),
    ]) {
      final queue = MemoryPlanWorkoutQueue();
      await queue.add(_pending('a'));
      final sync = PlanWorkoutSync(queue: queue, now: () => now, upload: (_) async => throw error);

      expect((await sync.flush('user-1')).remaining, 1, reason: '$error');
    }
  });

  test('older than 7 days is dropped; another user on the same phone is left alone', () async {
    final queue = MemoryPlanWorkoutQueue();
    await queue.add(_pending('old', at: now.subtract(const Duration(days: 8))));
    await queue.add(_pending('theirs', user: 'user-2'));
    final sent = <String>[];
    final sync = PlanWorkoutSync(queue: queue, now: () => now, upload: (w) async {
      sent.add(w.clientId);
      return PlanWorkoutOutcome.created;
    });

    final report = await sync.flush('user-1');

    expect(sent, isEmpty);
    expect(report.rejected, 1);
    expect(queue.all().map((w) => w.clientId), ['theirs']);
  });

  test('a pending workout survives a round trip through storage', () {
    final json = _pending('a').toJson();
    final back = PendingPlanWorkout.fromJson(jsonMap(json));
    expect(back.clientId, 'a');
    expect(back.userId, 'user-1');
    expect(back.finishedAt.toUtc(), DateTime(2026, 11, 23, 18).toUtc());
  });
}
