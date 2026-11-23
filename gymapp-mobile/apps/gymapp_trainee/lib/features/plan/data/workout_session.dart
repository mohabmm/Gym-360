import 'package:gymapp_core/gymapp_core.dart';

import 'plan_workout_queue.dart';

/// One set of one exercise in the workout (index 0 = set 1).
class SetRef {
  final String exerciseKey;
  final int index;

  const SetRef(this.exerciseKey, this.index);
}

/// The sets of one plan workout while the trainee does it: prefilled from the plan (reps,
/// time) and from the weight they used last time, ticked off one by one.
class WorkoutSession {
  final PlanSlot slot;
  final DateTime startedAt;
  final Map<String, List<CompletedSetModel>> _sets;

  WorkoutSession._(this.slot, this.startedAt, this._sets);

  factory WorkoutSession.start(PlanSlot slot, Map<String, double> lastWeights, {DateTime? now}) => WorkoutSession._(
        slot,
        now ?? DateTime.now(),
        {for (final e in slot.day.exercises) e.key: _prefill(e, lastWeights[e.key] ?? e.targetWeightKg)},
      );

  static List<CompletedSetModel> _prefill(PlanExerciseModel e, double? weight) => [
        for (var n = 1; n <= e.sets; n++)
          CompletedSetModel(
            exerciseKey: e.key,
            exerciseId: e.exerciseId,
            exerciseName: e.name,
            exerciseNameAr: e.nameAr,
            setNumber: n,
            reps: e.isTimed ? null : firstNumber(e.reps),
            seconds: e.seconds,
            weightKg: weight,
          ),
      ];

  /// "10-12" → 10, "AMRAP" → null.
  static int? firstNumber(String? reps) {
    final match = RegExp(r'\d+').firstMatch(reps ?? '');
    return match == null ? null : int.parse(match.group(0)!);
  }

  List<CompletedSetModel> setsOf(String exerciseKey) => List.unmodifiable(_sets[exerciseKey] ?? const []);

  int get doneCount => _sets.values.expand((s) => s).where((s) => s.done).length;

  int get totalCount => _sets.values.expand((s) => s).length;

  bool get hasProgress => doneCount > 0;

  void toggle(SetRef ref) => _update(ref, (s) => s.copyWith(done: !s.done));

  /// A new weight also fills the sets after it that still had the old one (done sets keep theirs).
  void setWeight(SetRef ref, double? kg) {
    final sets = _sets[ref.exerciseKey];
    if (sets == null || ref.index >= sets.length) return;
    final old = sets[ref.index].weightKg;
    sets[ref.index] = _withWeight(sets[ref.index], kg);
    for (var i = ref.index + 1; i < sets.length; i++) {
      if (_stillHas(sets[i], old)) sets[i] = _withWeight(sets[i], kg);
    }
  }

  static bool _stillHas(CompletedSetModel set, double? weight) => !set.done && set.weightKg == weight;

  void setReps(SetRef ref, int? reps) => _update(ref, (s) => s.copyWith(reps: reps));

  void _update(SetRef ref, CompletedSetModel Function(CompletedSetModel) change) {
    final sets = _sets[ref.exerciseKey];
    if (sets == null || ref.index >= sets.length) return;
    sets[ref.index] = change(sets[ref.index]);
  }

  static CompletedSetModel _withWeight(CompletedSetModel s, double? kg) => CompletedSetModel(
        exerciseKey: s.exerciseKey,
        exerciseId: s.exerciseId,
        exerciseName: s.exerciseName,
        exerciseNameAr: s.exerciseNameAr,
        setNumber: s.setNumber,
        done: s.done,
        reps: s.reps,
        seconds: s.seconds,
        weightKg: kg,
      );

  /// The upload, made once with a fresh client id so a retry can never count twice.
  PendingPlanWorkout finish({required String userId, required TrainingPlanModel plan, DateTime? now, String? clientId}) {
    final finishedAt = now ?? DateTime.now();
    final id = clientId ?? newClientId();
    return PendingPlanWorkout(
      clientId: id,
      userId: userId,
      planId: plan.summary.id,
      dayKey: slot.day.key,
      finishedAt: finishedAt,
      body: {
        'planId': plan.summary.id,
        'dayKey': slot.day.key,
        'completedAt': finishedAt.toUtc().toIso8601String(),
        'planVersion': plan.summary.version,
        'dayTitle': slot.day.title,
        'dayTitleAr': slot.day.titleAr,
        'week': slot.week,
        'sequenceIndex': slot.index,
        'durationMinutes': finishedAt.difference(startedAt).inMinutes.clamp(0, 600),
        'sets': [for (final s in _sets.values.expand((s) => s)) s.toJson()],
      },
    );
  }

  /// About how long the day takes: each set's work (≈40 s, or its time) plus its rest.
  static int estimatedMinutes(PlanDayModel day) {
    final seconds = day.exercises.fold<int>(0, (sum, e) => sum + e.sets * ((e.seconds ?? 40) + (e.restSeconds ?? 60)));
    return (seconds / 60).ceil();
  }
}
