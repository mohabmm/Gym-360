import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'board_fixtures.dart';

void main() {
  group('model', () {
    test('parses the summary and days', () {
      final board = trialBoard();

      expect(board.summary.isTrialView, isTrue);
      expect(board.summary.trialSessionsTotal, 20);
      expect(board.days.map((d) => d.label), ['Push Day', 'Pull Day']);
    });

    test('names follow the reader language', () {
      final bench = trialBoard().exercises.first;

      expect(bench.displayName(true), 'ضغط صدر');
      expect(bench.displayName(false), 'Bench press');
    });

    test('parses notes, targets and the trend', () {
      final board = trialBoard();
      final bench = board.exercises.first;

      expect(bench.entries.first.notes, 'Elbows in');
      expect(bench.entries.first.target?.weight, 62.5);
      expect(bench.nextTarget?.weight, 65);
    });

    test('missing target and trend flags stay empty', () {
      final row = trialBoard().exercises.last;

      expect(row.nextTarget, isNull);
      expect(row.trend.hitPreviousTarget, isNull);
    });

    test('a day lists only its exercises; no day lists all', () {
      final board = trialBoard();

      expect(board.exercisesFor(board.days.last).map((e) => e.key), ['barbell row']);
      expect(board.exercisesFor(null), hasLength(2));
    });

    test('empty JSON gives an empty trial board', () {
      final board = ExerciseProgressModel.fromJson(const {});

      expect(board.isEmpty, isTrue);
      expect(board.summary.isTrialView, isTrue);
    });
  });

  group('goal insights', () {
    test('each goal leads with its own number', () {
      final leads = [ProgressGoalType.buildMuscle, ProgressGoalType.loseFat, ProgressGoalType.generalFitness, null]
          .map(ExerciseLead.forGoal);

      expect(leads, [ExerciseLead.weight, ExerciseLead.volume, ExerciseLead.oneRepMax, ExerciseLead.oneRepMax]);
    });

    test('change compares first and latest for the goal', () {
      final bench = trialBoard().exercises.first;

      final weight = LeadChange.of(bench, ExerciseLead.weight);
      expect(weight.delta, 2.5);
      expect(weight.percent, closeTo(4.17, 0.01));
      expect(LeadChange.of(bench, ExerciseLead.volume).percent, closeTo(20.19, 0.01));
    });

    test('board counts improved exercises; one-off exercises are not compared', () {
      final insight = BoardInsight.of(trialBoard(), ProgressGoalType.buildMuscle);

      expect(insight.comparable, 1);
      expect(insight.improved, 1);
      expect(insight.isBaseline, isFalse);
    });

    test('board counts new bests and coach targets hit', () {
      final insight = BoardInsight.of(trialBoard(), ProgressGoalType.buildMuscle);

      expect(insight.personalBests, 1);
      expect(insight.targetsHit, 1);
    });

    test('headline follows the goal', () {
      final board = trialBoard();
      const en = ExerciseProgressStrings(false);

      expect(en.headline(BoardInsight.of(board, ProgressGoalType.buildMuscle)), 'Stronger on 1 of 1 exercises');
      expect(en.headline(BoardInsight.of(board, ProgressGoalType.loseFat)), contains('more energy burned'));
    });

    test('Arabic headline and English change line', () {
      final board = trialBoard();

      expect(const ExerciseProgressStrings(true).headline(BoardInsight.of(board, ProgressGoalType.buildMuscle)),
          'أصبحت أقوى في 1 من 1 تمارين');
      expect(const ExerciseProgressStrings(false).change(LeadChange.of(board.exercises.first, ExerciseLead.weight)),
          '60 kg → 62.5 kg in 2 sessions (+4.2%)');
    });
  });
}
