import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/exercises/session_exercises_draft.dart';

import '../exercise_progress_fixtures.dart';

void main() {
  test('repeating a day the client trained fills last time, using the coach target when set', () {
    final draft = SessionExercisesDraft();

    expect(draft.applyRepeat(trialBoard(), 'Push Day'), isTrue);

    expect(draft.dayLabel, 'Push Day');
    final bench = draft.rows.single;
    expect(bench.name, 'Bench press');
    expect(bench.sets, 3);
    expect(bench.reps, '10');
    expect(bench.weight, 65);
    expect(draft.canSave, isTrue);
  });

  test('a new day keeps the blank form; typed rows are never replaced', () {
    final fresh = SessionExercisesDraft();
    expect(fresh.applyRepeat(trialBoard(), 'Leg Day'), isFalse);
    expect(fresh.dayLabel, 'Leg Day');
    expect(fresh.rows.single.isBlank, isTrue);

    final typed = SessionExercisesDraft()..rows.single.name = 'Squat';
    expect(typed.applyRepeat(trialBoard(), 'Push Day'), isFalse);
    expect(typed.rows.single.name, 'Squat');
  });

  test('payload skips blank rows, trims text and sends the target only when set', () {
    final draft = SessionExercisesDraft(dayLabel: 'Pull Day', rows: [
      ExerciseRowDraft(name: ' Barbell row ', sets: 3, reps: '10,8,8', weight: 50, notes: ' Squeeze ', targetWeight: 52.5),
      ExerciseRowDraft(name: 'Face pull', sets: 3, reps: '15'),
      ExerciseRowDraft(),
    ]);

    final json = draft.toJson();
    final items = json['items'] as List;

    expect(json['dayLabel'], 'Pull Day');
    expect(items, hasLength(2));
    expect(items[0], {
      'exerciseName': 'Barbell row',
      'orderIndex': 0,
      'sets': 3,
      'reps': '10,8,8',
      'weight': 50,
      'notes': 'Squeeze',
      'target': {'sets': null, 'reps': null, 'weight': 52.5},
    });
    expect((items[1] as Map)['target'], isNull);
    expect((items[1] as Map)['orderIndex'], 1);
  });

  test('a row without reps cannot be saved', () {
    final draft = SessionExercisesDraft(rows: [ExerciseRowDraft(name: 'Squat', reps: '')]);

    expect(draft.canSave, isFalse);
  });
}
