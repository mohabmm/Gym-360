import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/plan/data/my_plan_view.dart';
import 'package:gymapp_trainee/features/plan/data/workout_session.dart';

import 'plan_fixtures.dart';

void main() {
  final plan = myPlan();
  final dayA = PlanSlot.of(plan.plan.document).first;

  test('sets are prefilled: reps from the plan, weight from last time, time for timed exercises', () {
    final session = WorkoutSession.start(dayA, plan.lastWeights);

    final squat = session.setsOf('e-squat-1');
    expect(squat, hasLength(2));
    expect(squat.first.reps, 10, reason: '"10-12" starts at 10');
    expect(squat.first.weightKg, 40);
    expect(session.setsOf('e-plank-1').single.seconds, 30);
    expect(session.totalCount, 3);
    expect(session.hasProgress, isFalse);
  });

  test('a new weight fills the next sets that still had the old one; done sets keep theirs', () {
    final session = WorkoutSession.start(dayA, plan.lastWeights)..toggle(const SetRef('e-squat-1', 0));

    session.setWeight(const SetRef('e-squat-1', 0), 45);
    expect(session.setsOf('e-squat-1').map((s) => s.weightKg), [45, 45]);

    session.setWeight(const SetRef('e-squat-1', 1), 47.5);
    expect(session.setsOf('e-squat-1').map((s) => s.weightKg), [45, 47.5]);
  });

  test('finishing makes one upload with a fresh client id and the day as the phone sees it', () {
    final started = DateTime.utc(2026, 11, 23, 17);
    final session = WorkoutSession.start(dayA, plan.lastWeights, now: started)
      ..toggle(const SetRef('e-squat-1', 0))
      ..toggle(const SetRef('e-plank-1', 0));

    final pending = session.finish(userId: 'user-1', plan: plan.plan, now: started.add(const Duration(minutes: 42)));

    expect(pending.clientId, hasLength(36));
    expect(pending.body['dayKey'], 'w1a');
    expect(pending.body['planVersion'], 3);
    expect(pending.body['sequenceIndex'], 0);
    expect(pending.body['durationMinutes'], 42);
    final sets = pending.body['sets'] as List;
    expect(sets.where((s) => (s as Map)['done'] == true), hasLength(2));
    expect(session.finish(userId: 'user-1', plan: plan.plan).clientId, isNot(pending.clientId));
  });

  test('a workout finished offline moves today\'s card on and counts this week', () {
    final session = WorkoutSession.start(dayA, plan.lastWeights)..toggle(const SetRef('e-squat-1', 0));
    final now = DateTime(2026, 11, 25, 19);
    final pending = session.finish(userId: 'user-1', plan: plan.plan, now: now);

    final view = MyPlanView.build(plan, [pending], now: now);

    expect(view.next!.day.key, 'w1b');
    expect(view.cursor.doneToday, isTrue);
    expect(view.stats.thisWeek, 1);
    expect(view.pendingCount, 1);
  });

  test('estimated minutes add work and rest', () {
    expect(WorkoutSession.estimatedMinutes(dayA.day), 5); // 2×(40+60) + 1×(30+30) = 260 s, rounded up
    expect(WorkoutSession.firstNumber('AMRAP'), isNull);
  });
}
