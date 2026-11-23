import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

PlanDocumentModel _twoWeeks() => const PlanDocumentModel(weeks: [
      PlanWeekModel(week: 2, days: [PlanDayModel(key: 'w2a', title: 'A'), PlanDayModel(key: 'w2b', title: 'B')]),
      PlanWeekModel(week: 1, days: [PlanDayModel(key: 'w1a', title: 'A'), PlanDayModel(key: 'w1b', title: 'B')]),
    ]);

void main() {
  test('slots run week by week in order', () {
    final slots = PlanSlot.of(_twoWeeks());
    expect(slots.map((s) => s.day.key), ['w1a', 'w1b', 'w2a', 'w2b']);
    expect(slots.map((s) => s.index), [0, 1, 2, 3]);
  });

  test('a workout finished offline moves the next day on before it is uploaded', () {
    const server = PlanCursorModel(nextDayKey: 'w1b', lastDoneIndex: 0, total: 4, doneKeys: ['w1a']);

    final local = server.withPending(_twoWeeks(), ['w1b'], pendingToday: true);

    expect(local.nextDayKey, 'w2a');
    expect(local.doneToday, isTrue);
    expect(local.doneKeys, containsAll(['w1a', 'w1b']));
  });

  test('redoing an earlier day offline does not go back, and the last day finishes the plan', () {
    const server = PlanCursorModel(nextDayKey: 'w2b', lastDoneIndex: 2, total: 4);

    expect(server.withPending(_twoWeeks(), ['w1a']).nextDayKey, 'w2b');
    final done = server.withPending(_twoWeeks(), ['w2b']);
    expect(done.finished, isTrue);
    expect(done.doneCount, 4);
  });

  test('a pending day the coach removed is ignored', () {
    const server = PlanCursorModel(nextDayKey: 'w1a', total: 4);
    expect(server.withPending(_twoWeeks(), ['gone']).nextDayKey, 'w1a');
  });

  test('my plan parses with last weights and outcomes parse', () {
    final model = MyPlanModel.fromJson({
      'plan': {
        'summary': {'id': 'p1', 'name': 'P', 'startDate': '2026-11-21', 'updatedAt': '2026-11-21T00:00:00Z'},
        'document': {'weeks': []},
      },
      'cursor': {'nextDayKey': 'd1', 'lastDoneIndex': -1, 'total': 3, 'doneToday': false, 'doneKeys': []},
      'stats': {'thisWeek': 1, 'weekTarget': 3, 'thisMonth': 4, 'lastWorkoutOn': '2026-11-22'},
      'lastWeights': {'e1': 42.5},
    });
    expect(model.lastWeights['e1'], 42.5);
    expect(model.stats.plus(week: 1, month: 1).thisWeek, 2);
    expect(PlanWeekStatsModel.weekStart(DateTime(2026, 11, 27)), DateTime(2026, 11, 21));
    expect(PlanWeekStatsModel.weekStart(DateTime(2026, 11, 28, 9)), DateTime(2026, 11, 28));
    expect(PlanWorkoutOutcome.parse('plan_replaced'), PlanWorkoutOutcome.planReplaced);
  });

  test('client ids are v4 uuids', () {
    final id = newClientId();
    expect(RegExp(r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$').hasMatch(id), isTrue);
    expect(newClientId(), isNot(id));
  });
}
