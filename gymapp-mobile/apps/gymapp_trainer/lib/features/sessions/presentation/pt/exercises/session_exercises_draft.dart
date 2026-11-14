import 'package:gymapp_core/gymapp_core.dart';

/// One exercise the coach is recording (mutable while the form is open).
class ExerciseRowDraft {
  String name;
  int sets;
  String reps;
  double? weight;
  String notes;
  int? targetSets;
  String targetReps;
  double? targetWeight;

  ExerciseRowDraft({
    this.name = '',
    this.sets = 3,
    this.reps = '',
    this.weight,
    this.notes = '',
    this.targetSets,
    this.targetReps = '',
    this.targetWeight,
  });

  /// Last time's numbers, or the coach's target for today when one was set.
  factory ExerciseRowDraft.fromHistory(ExerciseHistoryModel exercise) {
    final latest = exercise.latest;
    final target = exercise.nextTarget;
    return ExerciseRowDraft(
      name: exercise.name,
      sets: target?.sets ?? latest?.sets ?? 3,
      reps: (target?.reps?.trim().isNotEmpty ?? false) ? target!.reps!.trim() : latest?.reps ?? '',
      weight: target?.weight ?? latest?.weight,
    );
  }

  bool get isBlank => name.trim().isEmpty;

  bool get isValid => !isBlank && sets >= 1 && reps.trim().isNotEmpty;

  bool get hasTarget => targetSets != null || targetReps.trim().isNotEmpty || targetWeight != null;

  Map<String, dynamic> toJson(int orderIndex) => {
        'exerciseName': name.trim(),
        'orderIndex': orderIndex,
        'sets': sets,
        'reps': reps.trim(),
        'weight': weight,
        'notes': notes.trim().isEmpty ? null : notes.trim(),
        'target': hasTarget
            ? {'sets': targetSets, 'reps': targetReps.trim().isEmpty ? null : targetReps.trim(), 'weight': targetWeight}
            : null,
      };
}

/// The session form: a day label and the exercises.
class SessionExercisesDraft {
  String? dayLabel;
  final List<ExerciseRowDraft> rows;

  SessionExercisesDraft({this.dayLabel, List<ExerciseRowDraft>? rows}) : rows = rows ?? [ExerciseRowDraft()];

  /// Rows from the last session of [label] on the client's board; empty when that day is new.
  static List<ExerciseRowDraft> repeatOf(ExerciseProgressModel? board, String label) {
    final day = board?.days.where((d) => d.label == label).firstOrNull;
    if (board == null || day == null) return const [];
    return board.exercisesFor(day).map(ExerciseRowDraft.fromHistory).toList();
  }

  /// Fills the form from the last [label] day while nothing has been typed yet.
  bool applyRepeat(ExerciseProgressModel? board, String label) {
    dayLabel = label;
    final repeat = repeatOf(board, label);
    if (repeat.isEmpty || rows.any((r) => !r.isBlank)) return false;
    rows
      ..clear()
      ..addAll(repeat);
    return true;
  }

  List<ExerciseRowDraft> get filled => rows.where((r) => !r.isBlank).toList();

  bool get canSave => filled.isNotEmpty && filled.every((r) => r.isValid);

  Map<String, dynamic> toJson() => {
        'dayLabel': dayLabel,
        'items': [for (final (i, row) in filled.indexed) row.toJson(i)],
      };
}
