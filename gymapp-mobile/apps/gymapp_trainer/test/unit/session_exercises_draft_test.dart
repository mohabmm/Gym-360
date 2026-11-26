import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_draft.dart';

import '../exercise_progress_fixtures.dart';

List<String> _reps(ExerciseRowDraft row) => row.sets.map((s) => s.reps).toList();

List<double?> _kg(ExerciseRowDraft row) => row.sets.map((s) => s.weight).toList();

void main() {
  test('repeating a day the client trained fills last time, using the coach target when set', () {
    final draft = SessionExercisesDraft();

    expect(draft.pickDay(trialBoard(), 'Push Day'), DayPick.filled);

    final bench = draft.rows.single;
    expect((draft.dayLabel, bench.name, bench.setCount), ('Push Day', 'Bench press', 3));
    expect([..._reps(bench), ..._kg(bench)], ['10', '10', '10', 65, 65, 65]);
    expect(draft.canSave, isTrue);
  });

  test('a new day keeps the blank form; typed rows are never replaced without asking', () {
    final fresh = SessionExercisesDraft();
    expect(fresh.pickDay(trialBoard(), 'Leg Day'), DayPick.labelled);
    expect((fresh.dayLabel, fresh.rows.single.isUntouched), ('Leg Day', true));

    final typed = SessionExercisesDraft()..rows.single.name = 'Squat';
    expect(typed.pickDay(trialBoard(), 'Push Day'), DayPick.needsConfirm);
    expect((typed.dayLabel, typed.rows.single.name), (null, 'Squat'), reason: 'nothing changes until the coach agrees');

    expect(typed.pickDay(trialBoard(), 'Push Day', replace: true), DayPick.filled);
    expect(typed.rows.single.name, 'Bench press');
  });

  test('switching from a filled day to another swaps the template without going back', () {
    final draft = SessionExercisesDraft()..pickDay(trialBoard(), 'Push Day');

    expect(draft.pickDay(trialBoard(), 'Pull Day'), DayPick.filled);
    expect(draft.rows.single.name, 'Barbell row');

    expect(draft.pickDay(trialBoard(), 'Leg Day'), DayPick.cleared, reason: 'Pull Day rows go, Leg Day starts fresh');
    expect((draft.dayLabel, draft.rows.single.isUntouched), ('Leg Day', true));
  });

  test('no day: an untouched template is cleared; the coach\'s edits are kept', () {
    final draft = SessionExercisesDraft()..pickDay(trialBoard(), 'Push Day');
    expect(draft.clearDay(), isTrue);
    expect((draft.dayLabel, draft.rows.single.isUntouched), (null, true));

    final edited = SessionExercisesDraft()..pickDay(trialBoard(), 'Push Day');
    edited.rows.single.sets.first.reps = '12';
    expect(edited.isUnchangedPrefill, isFalse);
    expect(edited.clearDay(), isFalse);
    expect((edited.dayLabel, edited.rows.single.name), (null, 'Bench press'));
  });

  test('start over and undo bring back exactly what was there', () {
    final draft = SessionExercisesDraft()..pickDay(trialBoard(), 'Push Day');
    expect(draft.canClear, isTrue);
    final before = draft.snapshot();

    draft.startEmpty();
    expect((draft.dayLabel, draft.rows.single.isUntouched, draft.canClear), ('Push Day', true, false));

    draft.restore(before);
    expect((draft.dayLabel, draft.rows.single.name, draft.isUnchangedPrefill), ('Push Day', 'Bench press', true));
  });

  test("the coach's own named days are offered again, most recent first", () {
    final board = ExerciseProgressModel(
      summary: trialBoard().summary,
      days: [
        TrainingDayModel(label: 'Arms', lastTrained: DateTime(2026, 11, 1)),
        const TrainingDayModel(label: 'Push Day'),
        TrainingDayModel(label: 'Glutes & core', lastTrained: DateTime(2026, 11, 9)),
        const TrainingDayModel(),
      ],
      exercises: const [],
    );
    expect(SessionExercisesDraft.pastCustomDays(board), ['Glutes & core', 'Arms']);
    expect(SessionExercisesDraft.pastCustomDays(null), isEmpty);
  });

  test('payload sends each set with its own weight, the summary and the target', () {
    final press = ExerciseRowDraft(
      name: ' Leg press ',
      notes: ' Slow down ',
      targetWeight: 85,
      sets: [
        SetDraft(reps: '12', weight: 60, note: 'Good depth'),
        SetDraft(reps: '10', weight: 70, note: '   '),
        SetDraft(reps: '8', weight: 80, note: ' Last 2 reps assisted '),
      ],
    );
    final draft = SessionExercisesDraft(dayLabel: 'Leg Day', rows: [press, ExerciseRowDraft()]);

    final items = draft.toJson()['items'] as List;

    expect(items.single, {
      'exerciseName': 'Leg press',
      'orderIndex': 0,
      'measure': 'reps',
      'setEntries': [
        {'reps': 12, 'seconds': null, 'weight': 60.0, 'note': 'Good depth'},
        {'reps': 10, 'seconds': null, 'weight': 70.0, 'note': null},
        {'reps': 8, 'seconds': null, 'weight': 80.0, 'note': 'Last 2 reps assisted'},
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

  test('a new set repeats the last numbers but starts without a note', () {
    final row = ExerciseRowDraft(sets: [SetDraft(reps: '8', weight: 80, note: 'Assisted')])..setCount = 2;

    expect(row.sets.last.reps, '8');
    expect(row.sets.last.weight, 80);
    expect(row.sets.last.note, isEmpty);
  });

  test('a note alone keeps the exercise from being skipped as untouched', () {
    expect(ExerciseRowDraft(sets: [SetDraft(note: 'Watch knees')]).isUntouched, isFalse);
  });
}
