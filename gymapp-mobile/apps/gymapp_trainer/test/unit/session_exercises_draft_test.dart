import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_draft.dart';

import '../exercise_progress_fixtures.dart';

List<String> _reps(ExerciseRowDraft row) => row.sets.map((s) => s.reps).toList();

List<double?> _kg(ExerciseRowDraft row) => row.sets.map((s) => s.weight).toList();

void main() {
  test('repeating a day the client trained fills last time, using the coach target when set', () {
    final draft = SessionExercisesDraft();

    expect(draft.applyRepeat(trialBoard(), 'Push Day'), isTrue);

    final bench = draft.rows.single;
    expect((draft.dayLabel, bench.name, bench.setCount), ('Push Day', 'Bench press', 3));
    expect([..._reps(bench), ..._kg(bench)], ['10', '10', '10', 65, 65, 65]);
    expect(draft.canSave, isTrue);
  });

  test('a new day keeps the blank form; typed rows are never replaced', () {
    final fresh = SessionExercisesDraft();
    expect(fresh.applyRepeat(trialBoard(), 'Leg Day'), isFalse);
    expect((fresh.dayLabel, fresh.rows.single.isUntouched), ('Leg Day', true));

    final typed = SessionExercisesDraft()..rows.single.name = 'Squat';
    expect(typed.applyRepeat(trialBoard(), 'Push Day'), isFalse);
    expect(typed.rows.single.name, 'Squat');
  });

  test('payload sends each set with its own weight, the summary and the target', () {
    final press = ExerciseRowDraft(
      name: ' Leg press ',
      notes: ' Slow down ',
      targetWeight: 85,
      sets: [SetDraft(reps: '12', weight: 60), SetDraft(reps: '10', weight: 70), SetDraft(reps: '8', weight: 80)],
    );
    final draft = SessionExercisesDraft(dayLabel: 'Leg Day', rows: [press, ExerciseRowDraft()]);

    final items = draft.toJson()['items'] as List;

    expect(items.single, {
      'exerciseName': 'Leg press',
      'orderIndex': 0,
      'measure': 'reps',
      'setEntries': [
        {'reps': 12, 'seconds': null, 'weight': 60.0},
        {'reps': 10, 'seconds': null, 'weight': 70.0},
        {'reps': 8, 'seconds': null, 'weight': 80.0},
      ],
      'sets': 3,
      'reps': '12,10,8',
      'weight': 80.0,
      'notes': 'Slow down',
      'target': {'sets': null, 'reps': null, 'weight': 85.0, 'seconds': null},
    });
  });

  test('a plank is sent in seconds; "1:30" and Arabic digits are read as time', () {
    final plank = ExerciseRowDraft(
      name: 'Plank',
      measure: ExerciseMeasure.time,
      targetSeconds: 60,
      sets: [SetDraft(time: '45'), SetDraft(time: '1:30', weight: 10), SetDraft(time: '٤٠')],
    );

    final json = plank.toJson(0);

    expect((json['setEntries'] as List).map((s) => (s as Map)['seconds']), [45, 90, 40]);
    expect((json['measure'], json['reps']), ('time', '45s,90s,40s'));
    expect(json['target'], {'sets': null, 'reps': null, 'weight': null, 'seconds': 60});
  });

  test('time text: seconds, m:ss, and anything else is not a time', () {
    const cases = {'45': 45, '1:30': 90, '٢:٠٥': 125, '0:59': 59, '': null, '1:': null, '1:2:3': null};
    for (final MapEntry(key: text, value: seconds) in cases.entries) {
      expect(SetDraft.parseSeconds(text), seconds, reason: '"$text"');
    }
    expect([SetDraft.formatSeconds(45), SetDraft.formatSeconds(90)], ['45', '1:30']);
  });

  test('adding a set repeats the last one (reps and kg); dropping keeps the first ones', () {
    final row = ExerciseRowDraft(sets: [SetDraft(reps: '12', weight: 60), SetDraft(reps: '10', weight: 70)]);

    row.setCount = 3;
    expect([..._reps(row), ..._kg(row)], ['12', '10', '10', 60, 70, 70]);

    row.setCount = 0;
    expect(row.setCount, 1, reason: 'never fewer than one set');
    row.setCount = 50;
    expect(row.setCount, ExerciseRowDraft.maxSets);
  });

  test('legacy reps text expands set by set', () {
    const cases = [
      (3, '12', ['12', '12', '12']),
      (3, '10,8,8', ['10', '8', '8']),
      (4, '10,8', ['10', '8', '8', '8']),
      (2, '8-12', ['8', '8']),
      (2, '١٢،١٠', ['12', '10']),
    ];
    for (final (count, reps, expected) in cases) {
      expect(ExerciseRowDraft.expandReps(reps, count), expected, reason: '$count × "$reps"');
    }
  });

  test('missing reps or time name the sets that block saving', () {
    final squat = ExerciseRowDraft(name: 'Squat', sets: [SetDraft(reps: '5'), SetDraft(), SetDraft(reps: '5')]);
    final plank = ExerciseRowDraft(name: 'Plank', measure: ExerciseMeasure.time, sets: [SetDraft(time: '30'), SetDraft(reps: '30')]);

    expect(squat.issue, ExerciseRowIssue.missingSets);
    expect(squat.setsMissing, [2]);
    expect(plank.setsMissing, [2], reason: 'reps typed under a timed exercise do not count as time');
  });

  test('a row with numbers but no name blocks saving instead of being dropped', () {
    final draft = SessionExercisesDraft(rows: [
      ExerciseRowDraft.fromReps(name: 'Squat', reps: '5'),
      ExerciseRowDraft.fromReps(reps: '10'),
      ExerciseRowDraft(),
    ]);

    expect(draft.filled, hasLength(2));
    expect((draft.firstInvalid?.issue, draft.canSave), (ExerciseRowIssue.missingName, false));
  });
}
