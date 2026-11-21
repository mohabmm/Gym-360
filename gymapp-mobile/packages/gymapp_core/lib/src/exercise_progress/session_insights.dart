import 'exercise_progress_model.dart';
import 'session_summary_model.dart';

/// Change since the previous time the exercise was done: kilos on the top set, else reps,
/// or seconds held for a timed exercise. Every field null means "same as last time".
class ExerciseDelta {
  final double? kg;
  final int? reps;
  final int? seconds;
  final bool isFirstTime;

  const ExerciseDelta({this.kg, this.reps, this.seconds, this.isFirstTime = false});

  static const firstTime = ExerciseDelta(isFirstTime: true);

  factory ExerciseDelta.between(ExerciseEntryModel? previous, ExerciseEntryModel entry) {
    if (previous == null) return firstTime;
    if (entry.isTimed) return ExerciseDelta(seconds: _nonZero(_diff(entry.bestSeconds, previous.bestSeconds)));
    final kg = entry.weight != null && previous.weight != null ? entry.weight! - previous.weight! : 0.0;
    if (kg != 0) return ExerciseDelta(kg: kg);
    return ExerciseDelta(reps: _nonZero(entry.totalReps - previous.totalReps));
  }

  /// The change before [entry] on [exercise], against the last time it was counted the same way.
  factory ExerciseDelta.forEntry(ExerciseHistoryModel exercise, ExerciseEntryModel entry) {
    final index = exercise.entries.indexOf(entry);
    final previous = exercise.entries.take(index < 0 ? 0 : index).where((e) => e.isTimed == entry.isTimed).lastOrNull;
    return ExerciseDelta.between(previous, entry);
  }

  num? get _value => kg ?? reps ?? seconds;

  bool get isUp => (_value ?? 0) > 0;
  bool get isDown => (_value ?? 0) < 0;
  bool get isSame => !isFirstTime && _value == null;

  static int? _diff(int? a, int? b) => a == null || b == null ? null : a - b;
  static int? _nonZero(int? v) => v == 0 ? null : v;
}

/// One exercise as done in one session.
class SessionExerciseRow {
  final ExerciseHistoryModel exercise;
  final ExerciseEntryModel entry;
  final ExerciseDelta delta;

  const SessionExerciseRow({required this.exercise, required this.entry, required this.delta});
}

/// A session read back to the member: what they did, in the coach's order, and what went up.
class SessionRecap {
  final SessionSummaryModel session;
  final List<SessionExerciseRow> rows;

  const SessionRecap({required this.session, required this.rows});

  factory SessionRecap.of(ExerciseProgressModel board, SessionSummaryModel session) {
    final byKey = {for (final e in board.exercises) e.key: e};
    final rows = <SessionExerciseRow>[];
    for (final key in session.exerciseKeys) {
      final exercise = byKey[key];
      final entry = exercise?.entries.where((e) => e.sessionId == session.sessionId).firstOrNull;
      if (exercise == null || entry == null) continue;
      rows.add(SessionExerciseRow(exercise: exercise, entry: entry, delta: ExerciseDelta.forEntry(exercise, entry)));
    }
    return SessionRecap(session: session, rows: rows);
  }

  int get personalBests => rows.where((r) => r.entry.isPersonalBest).length;
  int get improved => rows.where((r) => r.delta.isUp).length;
  int get targetsHit => rows.where((r) => r.entry.hitPreviousTarget == true).length;
  bool get hasWins => personalBests + improved + targetsHit > 0;
}

/// What the coach lined up for next time: the latest session's targets and focus.
class NextSessionPlan {
  final String? focus;
  final List<SessionExerciseRow> targets;

  const NextSessionPlan({this.focus, this.targets = const []});

  /// Null when the coach set nothing for next time.
  static NextSessionPlan? of(ExerciseProgressModel board) {
    final latest = board.latestSession;
    if (latest == null) return null;
    final targets = SessionRecap.of(board, latest).rows.where((r) => r.entry.target != null).toList();
    final focus = latest.coachRecap?.nextSessionFocus;
    if (focus == null && targets.isEmpty) return null;
    return NextSessionPlan(focus: focus, targets: targets);
  }
}
