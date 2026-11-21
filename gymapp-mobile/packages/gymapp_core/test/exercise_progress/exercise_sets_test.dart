import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// pt.exercise_progress: weight per set and timed (plank) exercises on the board.
ExerciseEntryModel _entry(Map<String, dynamic> json) => ExerciseEntryModel.fromJson({
      'sessionId': 's',
      'date': '2026-11-21',
      'sessionNumber': 1,
      'sets': 3,
      'reps': '',
      ...json,
    });

ExerciseHistoryModel _plank(List<int> bestHolds) => ExerciseHistoryModel(
      key: 'plank',
      name: 'Plank',
      entries: [
        _entry({'reps': '60s', 'weight': null}), // logged before timed exercises existed
        for (final s in bestHolds)
          _entry({
            'measure': 'time',
            'bestSeconds': s,
            'setEntries': [
              {'seconds': s},
            ],
          }),
      ],
      trend: const ExerciseTrendModel(),
    );

void main() {
  const en = ExerciseProgressStrings(false);
  const ar = ExerciseProgressStrings(true);

  test('parses the measure, each set and the timed numbers', () {
    final e = _entry({
      'measure': 'time',
      'totalSeconds': 85,
      'bestSeconds': 45,
      'setEntries': [
        {'seconds': 45, 'weight': 10},
        {'seconds': 40},
      ],
      'target': {'seconds': 60},
    });

    expect(e.isTimed, isTrue);
    expect([e.totalSeconds, e.bestSeconds], [85, 45]);
    expect(e.setEntries.map((s) => (s.seconds, s.weight)), [(45, 10.0), (40, null)]);
    expect(e.target?.seconds, 60);
  });

  test('set line shows what was done in each set', () {
    final pyramid = _entry({
      'setEntries': [
        {'reps': 12, 'weight': 60},
        {'reps': 10, 'weight': 70},
        {'reps': 8, 'weight': 80},
      ],
    });
    final sameWeight = _entry({
      'setEntries': [
        {'reps': 10, 'weight': 60},
        {'reps': 8, 'weight': 60},
      ],
    });
    final plank = _entry({
      'measure': 'time',
      'setEntries': [
        {'seconds': 45},
        {'seconds': 90, 'weight': 10},
      ],
    });
    final legacy = _entry({'reps': '10,8,8', 'weight': 60});

    expect(
      [en.entryLine(pyramid), en.entryLine(sameWeight), en.entryLine(plank), en.entryLine(legacy)],
      ['12×60 · 10×70 · 8×80 kg', '2 × 10,8 @ 60 kg', '45s · 1:30 +10 kg', '3 × 10,8,8 @ 60 kg'],
    );
    expect(ar.entryLine(plank), '45ث · 1:30 +10 كجم');
  });

  test('a timed exercise is followed by its longest hold whatever the goal', () {
    final change = LeadChange.of(_plank([40, 60]), ExerciseLead.weight);

    expect([change.first, change.latest], [40, 60], reason: 'the old reps-style row is skipped');
    expect(change.isTimed, isTrue);
    expect(en.change(change), '40s → 1:00 in 3 sessions (+50%)');
  });

  test('a longer hold counts as improved on the board', () {
    final board = ExerciseProgressModel(
      summary: const ExerciseProgressSummaryModel(),
      days: const [],
      exercises: [_plank([30, 45])],
    );

    final insight = BoardInsight.of(board, ProgressGoalType.loseFat);
    expect([insight.improved, insight.comparable], [1, 1]);
  });

  test('a seconds target reads as a duration', () {
    expect(en.target(const ExerciseTargetModel(sets: 3, seconds: 60)), '3 × 1:00');
  });

  test('lists the coach note of each set, skipping sets without one', () {
    final lift = _entry({
      'measure': 'reps',
      'setEntries': [
        {'reps': 12, 'weight': 60, 'note': 'Good depth'},
        {'reps': 10, 'weight': 70},
        {'reps': 8, 'weight': 80, 'note': ' Last 2 reps assisted '},
      ],
    });
    expect(en.setNotes(lift), ['S1 (12×60 kg): Good depth', 'S3 (8×80 kg): Last 2 reps assisted']);
    expect(ar.setNotes(lift).first, 'م1 (12×60 كجم): Good depth');

    final plank = _entry({
      'measure': 'time',
      'setEntries': [
        {'seconds': 45, 'note': 'Hips dropped'},
      ],
    });
    expect(en.setNotes(plank), ['S1 (45s): Hips dropped']);
    expect(en.setNotes(_entry({'reps': '10,8,8', 'weight': 60})), isEmpty);
  });
}
