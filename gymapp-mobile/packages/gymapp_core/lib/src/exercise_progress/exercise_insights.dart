import '../progress_journey/progress_journey_model.dart';
import 'exercise_progress_model.dart';

/// Which number tells the member's story best for their goal.
enum ExerciseLead {
  /// Build muscle: the working weight on the bar.
  weight,

  /// Lose fat: total work done (reps × weight) — more work, more energy burned.
  volume,

  /// General fitness (or no goal yet): estimated one-rep max, an all-round strength score.
  oneRepMax;

  static ExerciseLead forGoal(ProgressGoalType? goal) => switch (goal) {
        ProgressGoalType.buildMuscle => ExerciseLead.weight,
        ProgressGoalType.loseFat => ExerciseLead.volume,
        _ => ExerciseLead.oneRepMax,
      };

  double? of(ExerciseEntryModel entry) => switch (this) {
        ExerciseLead.weight => entry.weight,
        ExerciseLead.volume => entry.volume,
        ExerciseLead.oneRepMax => entry.estimatedOneRepMax,
      };
}

/// First → latest value of the lead number for one exercise.
class LeadChange {
  final double? first;
  final double? latest;
  final int sessions;

  const LeadChange({this.first, this.latest, this.sessions = 0});

  factory LeadChange.of(ExerciseHistoryModel exercise, ExerciseLead lead) {
    final values = exercise.entries.map(lead.of).whereType<double>().toList();
    return LeadChange(
      first: values.isEmpty ? null : values.first,
      latest: values.isEmpty ? null : values.last,
      sessions: exercise.entries.length,
    );
  }

  bool get isComparable => first != null && latest != null && sessions > 1;

  double? get delta => isComparable ? latest! - first! : null;

  double? get percent => isComparable && first! > 0 ? (latest! - first!) / first! * 100 : null;

  bool get improved => (delta ?? 0) > 0;
}

/// Board-wide story for the member's goal: how many exercises moved the right way.
class BoardInsight {
  final ExerciseLead lead;
  final int improved;
  final int comparable;
  final int personalBests;
  final int targetsHit;

  const BoardInsight({
    required this.lead,
    this.improved = 0,
    this.comparable = 0,
    this.personalBests = 0,
    this.targetsHit = 0,
  });

  factory BoardInsight.of(ExerciseProgressModel board, ProgressGoalType? goal) {
    final lead = ExerciseLead.forGoal(goal);
    final changes = board.exercises.map((e) => LeadChange.of(e, lead)).where((c) => c.isComparable).toList();
    return BoardInsight(
      lead: lead,
      improved: changes.where((c) => c.improved).length,
      comparable: changes.length,
      personalBests: board.exercises.where((e) => e.trend.latestIsPersonalBest).length,
      targetsHit: board.exercises.where((e) => e.trend.hitPreviousTarget == true).length,
    );
  }

  /// Nothing to compare yet: every exercise was done once.
  bool get isBaseline => comparable == 0;
}
