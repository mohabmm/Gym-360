import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'journey_fixtures.dart';

void main() {
  group('day one baseline', () {
    test('is the first coach measurement, even after an earlier self-logged entry', () {
      final j = journeyOf([
        weighIn('2026-09-10', 95),
        weighIn('2026-09-01', 97, self: true),
        weighIn('2026-10-10', 91),
      ]);

      final weight = j.change(ProgressMetric.weight)!;
      expect(
        (j.startDate, j.baselineIsSelfReported, weight.first, weight.latest),
        (DateTime(2026, 9, 10), false, 95.0, 91.0),
      );
      expect(j.timeline.length, 3, reason: 'the earlier self entry stays in the timeline');
      expect(j.series(ProgressMetric.weight).map((p) => p.value), [95, 91]);
    });

    test('falls back to the earliest entry, flagged self-reported, when no coach entry exists', () {
      final j = journeyOf([
        weighIn('2026-09-05', 80, self: true),
        weighIn('2026-09-01', 82, self: true),
      ]);

      expect(j.startDate, DateTime(2026, 9, 1));
      expect(j.baselineIsSelfReported, isTrue);
      expect(j.change(ProgressMetric.weight)!.delta, closeTo(-2, 1e-9));
    });

    test('empty journey has no baseline and no changes', () {
      final j = journeyOf([]);

      expect(
        (j.isEmpty, j.timeline.length, j.change(ProgressMetric.weight), j.daysSinceStart(DateTime(2026, 11, 13))),
        (true, 0, null, 0),
      );
    });

    test('a single check-in is a baseline with nothing to compare yet', () {
      final j = journeyOf([entry('2026-11-01', {ProgressMetric.weight: 90, ProgressMetric.waist: 100})]);

      expect((j.hasOnlyBaseline, j.change(ProgressMetric.weight), j.timeline.single.isDayOne), (true, null, true));
      expect(j.trackedGroups, [MetricGroup.weight, MetricGroup.waist]);
    });
  });

  group('per-metric change', () {
    test('uses the latest entry that has the metric', () {
      final j = journeyOf([
        entry('2026-09-01', {ProgressMetric.weight: 95, ProgressMetric.waist: 104}),
        entry('2026-10-01', {ProgressMetric.weight: 92, ProgressMetric.waist: 100}),
        weighIn('2026-11-01', 90), // no tape measurement this time
      ]);

      expect(j.change(ProgressMetric.weight)!.latest, 90);
      expect(j.change(ProgressMetric.waist)!.latest, 100);
      expect(j.change(ProgressMetric.waist)!.unit, 'cm');
    });

    test('metric measured only once has no change', () {
      final j = journeyOf([
        entry('2026-09-01', {ProgressMetric.weight: 95, ProgressMetric.leftArm: 35}),
        weighIn('2026-10-01', 92),
      ]);

      expect(j.change(ProgressMetric.leftArm), isNull);
      expect(j.trackedGroups, contains(MetricGroup.arms));
    });

    test('days since day one counts calendar days', () {
      final j = journeyOf([weighIn('2026-10-10', 95)]);

      expect(j.daysSinceStart(DateTime(2026, 11, 13, 23, 59)), 34);
    });
  });

  group('goal colors', () {
    final entries = [
      entry('2026-09-01', {ProgressMetric.weight: 95, ProgressMetric.waist: 104, ProgressMetric.leftArm: 34}),
      entry('2026-11-01', {ProgressMetric.weight: 91, ProgressMetric.waist: 99, ProgressMetric.leftArm: 36}),
    ];

    test('lose fat: weight and waist down are good; arms neutral', () {
      final j = journeyOf(entries, goal: ProgressGoalType.loseFat);

      expect(j.change(ProgressMetric.weight)!.tone, ChangeTone.good);
      expect(j.change(ProgressMetric.waist)!.tone, ChangeTone.good);
      expect(j.change(ProgressMetric.leftArm)!.tone, ChangeTone.neutral);
    });

    test('build muscle: weight down is bad, arms up are good', () {
      final j = journeyOf(entries, goal: ProgressGoalType.buildMuscle);

      expect(j.change(ProgressMetric.weight)!.tone, ChangeTone.bad);
      expect(j.change(ProgressMetric.leftArm)!.tone, ChangeTone.good);
    });

    test('no goal or general fitness: everything neutral', () {
      for (final goal in [null, ProgressGoalType.generalFitness]) {
        final j = journeyOf(entries, goal: goal);
        expect(j.change(ProgressMetric.weight)!.tone, ChangeTone.neutral);
      }
    });

    test('tiny changes count as no change', () {
      expect(ProgressMetric.weight.toneFor(0.01, ProgressGoalType.loseFat), ChangeTone.neutral);
    });
  });

  group('target weight', () {
    test('progress is the share of the way from day one to the target', () {
      final j = journeyOf([
        weighIn('2026-09-01', 100),
        weighIn('2026-11-01', 95),
      ], goal: ProgressGoalType.loseFat, target: 90);

      expect(j.targetProgress, closeTo(0.5, 1e-9));
    });

    test('is clamped and null without a target', () {
      final overshoot = journeyOf([weighIn('2026-09-01', 100), weighIn('2026-11-01', 85)],
          goal: ProgressGoalType.loseFat, target: 90);
      final noTarget = journeyOf([weighIn('2026-09-01', 100)], goal: ProgressGoalType.loseFat);
      final wrongWay = journeyOf([weighIn('2026-09-01', 100), weighIn('2026-11-01', 103)],
          goal: ProgressGoalType.loseFat, target: 90);

      expect(overshoot.targetProgress, 1.0);
      expect(noTarget.targetProgress, isNull);
      expect(wrongWay.targetProgress, 0.0);
    });
  });

  test('timeline is newest first with changes since the previous check-in', () {
    final j = journeyOf([
      weighIn('2026-09-01', 95),
      weighIn('2026-10-01', 93),
      weighIn('2026-11-01', 90),
    ]);

    final timeline = j.timeline;
    expect(timeline.map((c) => c.date.month), [11, 10, 9]);
    expect(timeline.first.changes.single.delta, closeTo(-3, 1e-9));
    expect((timeline.last.changes.length, timeline.last.isDayOne), (0, true));
  });

  group('model parsing', () {
    test('parses the journey response with an unknown goal as no goal', () {
      final model = ProgressJourneyModel.fromJson({
        'userId': 'member-1',
        'entries': [
          {
            'id': 'e1',
            'userId': 'member-1',
            'gymId': 'gym-1',
            'date': '2026-09-01',
            'weight': 95.5,
            'weightUnit': 'Kg',
            'source': 'TrainerMeasured',
            'loggedBy': 'coach-1',
            'createdAt': '2026-09-01T10:00:00Z',
          },
        ],
        'goal': {'goal': 'Bulk', 'targetWeight': 80},
      });

      expect(model.entries.single.weight, 95.5);
      expect(model.goal, isNull);
    });

    test('goal type round-trips the API value case-insensitively', () {
      expect(ProgressGoalType.tryParse('losefat'), ProgressGoalType.loseFat);
      expect(ProgressGoalType.buildMuscle.apiValue, 'BuildMuscle');
    });
  });
}
