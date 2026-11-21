import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// The member's session detail reads each set and the coach's note on it.
void main() {
  Map<String, dynamic> log([Map<String, dynamic> extra = const {}]) => {
        'id': 'l1',
        'sessionId': 's1',
        'exerciseName': 'Leg press',
        'orderIndex': 0,
        'sets': 3,
        'reps': '12,10,8',
        'createdAt': '2026-11-21T18:00:00Z',
        ...extra,
      };

  test('parses the measure and each set with its note', () {
    final model = SessionExerciseLogModel.fromJson(log({
      'measure': 'reps',
      'setEntries': [
        {'reps': 12, 'weight': 60, 'note': 'Good depth'},
        {'reps': 10, 'weight': 70, 'note': null},
        {'reps': 8, 'weight': 80, 'note': 'Last 2 reps assisted'},
      ],
    }));

    expect(model.measure, 'reps');
    expect(model.setEntries.map((s) => s.weight), [60, 70, 80]);
    expect(model.setEntries.map((s) => s.hasNote), [true, false, true]);
    expect(model.setEntries.last.note, 'Last 2 reps assisted');
  });

  test('a log from before per-set logging has no sets and reads as reps', () {
    final model = SessionExerciseLogModel.fromJson(log());

    expect(model.measure, 'reps');
    expect(model.setEntries, isEmpty);
  });
}
