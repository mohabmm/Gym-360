import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'board_fixtures.dart';

ExerciseEntryModel _lift({double? weight, int totalReps = 30, String id = 's'}) =>
    ExerciseEntryModel(sessionId: id, date: DateTime(2026, 11, 1), sessionNumber: 1, sets: 3, reps: '10', totalReps: totalReps, weight: weight);

ExerciseEntryModel _hold(int seconds) => ExerciseEntryModel(
    sessionId: 's', date: DateTime(2026, 11, 1), sessionNumber: 1, sets: 3, reps: '', measure: ExerciseMeasure.time, bestSeconds: seconds);

void main() {
  group('ExerciseDelta', () {
    test('first time, kilos, then reps when the weight held, then same', () {
      expect(ExerciseDelta.between(null, _lift(weight: 60)).isFirstTime, isTrue);
      expect(ExerciseDelta.between(_lift(weight: 60), _lift(weight: 62.5)).kg, 2.5);
      final reps = ExerciseDelta.between(_lift(weight: 60, totalReps: 26), _lift(weight: 60, totalReps: 30));
      expect(reps.reps, 4);
      expect(reps.isUp, isTrue);
      expect(ExerciseDelta.between(_lift(weight: 60), _lift(weight: 60)).isSame, isTrue);
      expect(ExerciseDelta.between(_lift(weight: 60), _lift(weight: 55)).isDown, isTrue);
    });

    test('timed exercises compare the longest hold', () {
      expect(ExerciseDelta.between(_hold(40), _hold(50)).seconds, 10);
    });

    test('labels in both languages', () {
      const en = ExerciseProgressStrings(false);
      const ar = ExerciseProgressStrings(true);
      expect(en.delta(const ExerciseDelta(kg: 2.5)), '+2.5 kg');
      expect(en.delta(const ExerciseDelta(kg: -5)), '−5 kg');
      expect(en.delta(const ExerciseDelta(reps: 1)), '+1 rep');
      expect(en.delta(const ExerciseDelta(seconds: 10)), '+10s');
      expect(en.delta(ExerciseDelta.firstTime), 'First time');
      expect(ar.delta(const ExerciseDelta(kg: 2.5)), '+2.5 كجم');
      expect(ar.delta(const ExerciseDelta()), 'نفس المرة السابقة');
    });
  });

  group('SessionRecap', () {
    test('rows follow the coach order with the change since last time and the wins', () {
      final board = trialBoard();
      final recap = SessionRecap.of(board, board.latestSession!);

      expect(recap.rows.map((r) => r.exercise.key), ['bench press']);
      expect(recap.rows.single.delta.kg, 2.5);
      expect(recap.personalBests, 1);
      expect(recap.improved, 1);
      expect(recap.targetsHit, 1);
      expect(board.latestSession!.coachRecap!.effortRating, 4);
    });

    test('older servers without sessions: rebuilt from the entries', () {
      final board = trialBoard(withSessions: false);

      expect(board.sessions.map((s) => s.sessionNumber), [1, 2, 3]);
      expect(board.sessions.last.exerciseKeys, ['bench press']);
      expect(board.sessions.last.coachRecap, isNull);
    });
  });

  test('NextSessionPlan: the latest targets and the coach focus', () {
    final plan = NextSessionPlan.of(trialBoard())!;

    expect(plan.focus, 'Add 2.5 kg on bench');
    expect(plan.targets.single.exercise.name, 'Bench press');
    expect(NextSessionPlan.of(ExerciseProgressModel.fromJson(const {})), isNull);
  });
}
