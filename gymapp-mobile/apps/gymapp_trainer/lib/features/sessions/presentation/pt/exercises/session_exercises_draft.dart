import 'dart:convert';

import 'package:gymapp_core/gymapp_core.dart';

/// What stops one exercise from being saved.
enum ExerciseRowIssue { missingName, missingSets }

/// One set as the member did it, as typed: reps (reps exercise) or time (timed exercise), kg optional,
/// and the coach's own note on that set.
class SetDraft {
  /// Mirrors ExerciseSetEntry.MaxNoteLength on the backend.
  static const int maxNoteLength = 200;

  String reps;
  String time;
  double? weight;
  String note;

  SetDraft({this.reps = '', this.time = '', this.weight, this.note = ''});

  /// The next set starts from this one's numbers; the note is about this set only.
  SetDraft copy() => SetDraft(reps: reps, time: time, weight: weight);

  bool get isEmpty => reps.trim().isEmpty && time.trim().isEmpty && weight == null && note.trim().isEmpty;

  int? get repsValue => int.tryParse(reps.trim());

  int? get seconds => SetDraft.parseSeconds(time);

  bool isDone(ExerciseMeasure measure) {
    final value = measure == ExerciseMeasure.time ? seconds : repsValue;
    final max = measure == ExerciseMeasure.time ? ExerciseRowDraft.maxSeconds : ExerciseRowDraft.maxReps;
    return value != null && value >= 1 && value <= max;
  }

  Map<String, dynamic> toJson(ExerciseMeasure measure) => {
        'reps': measure == ExerciseMeasure.reps ? repsValue : null,
        'seconds': measure == ExerciseMeasure.time ? seconds : null,
        'weight': weight,
        'note': note.trim().isEmpty ? null : note.trim(),
      };

  /// "45" → 45 s, "1:30" → 90 s; Arabic digits accepted. Null when it is not a time.
  static int? parseSeconds(String text) {
    final parts = ExerciseRowDraft.westernDigits(text.trim()).split(':');
    if (parts.length > 2 || parts.any((p) => p.isEmpty)) return null;
    final numbers = parts.map(int.tryParse).toList();
    if (numbers.contains(null)) return null;
    return parts.length == 1 ? numbers[0] : numbers[0]! * 60 + numbers[1]!;
  }

  /// 45 → "45", 90 → "1:30" — how a time is shown back in the field.
  static String formatSeconds(int seconds) =>
      seconds < 60 ? '$seconds' : '${seconds ~/ 60}:${(seconds % 60).toString().padLeft(2, '0')}';
}

/// One exercise the coach is recording (mutable while the form is open): how it is counted and
/// each set as done — "12×60 · 10×70 · 8×80" or a plank's "0:45 · 0:40".
class ExerciseRowDraft {
  static const int defaultSets = 3;

  /// Mirror the API limits (ExerciseSetEntry on the backend).
  static const int maxSets = 10;
  static const int maxReps = 999;
  static const int maxSeconds = 999;

  String name;
  ExerciseMeasure measure;
  final List<SetDraft> sets;
  String notes;
  int? targetSets;
  String targetReps;
  double? targetWeight;
  int? targetSeconds;

  ExerciseRowDraft({
    this.name = '',
    this.measure = ExerciseMeasure.reps,
    List<SetDraft>? sets,
    this.notes = '',
    this.targetSets,
    this.targetReps = '',
    this.targetWeight,
    this.targetSeconds,
  }) : sets = sets ?? List.generate(defaultSets, (_) => SetDraft());

  /// Legacy text form: "10,8,8" over [count] sets at one [weight].
  factory ExerciseRowDraft.fromReps({String name = '', int count = defaultSets, String reps = '', double? weight}) =>
      ExerciseRowDraft(
        name: name,
        sets: [for (final r in expandReps(reps, count)) SetDraft(reps: r, weight: weight)],
      );

  /// Last time's sets, moved toward the coach's target for today when one was set.
  factory ExerciseRowDraft.fromHistory(ExerciseHistoryModel exercise) {
    final latest = exercise.latest;
    final row = ExerciseRowDraft(
      name: exercise.name,
      measure: latest?.measure ?? ExerciseMeasure.reps,
      sets: latest == null ? null : _setsOf(latest),
    );
    final target = exercise.nextTarget;
    if (target != null) row._applyTarget(target);
    return row;
  }

  static List<SetDraft> _setsOf(ExerciseEntryModel entry) {
    if (entry.setEntries.isEmpty) {
      return [for (final r in expandReps(entry.reps, entry.sets)) SetDraft(reps: r, weight: entry.weight)];
    }
    return [
      for (final s in entry.setEntries.take(maxSets))
        SetDraft(
          reps: s.reps?.toString() ?? '',
          time: s.seconds == null ? '' : SetDraft.formatSeconds(s.seconds!),
          weight: s.weight,
        ),
    ];
  }

  void _applyTarget(ExerciseTargetModel target) {
    if (target.sets != null) setCount = target.sets!;
    final reps = _leadingDigits(westernDigits(target.reps ?? ''));
    final seconds = target.seconds;
    final topWeight = _topWeight;
    for (final s in sets) {
      if (reps.isNotEmpty) s.reps = reps;
      if (seconds != null) s.time = SetDraft.formatSeconds(seconds);
      // The target weight is the top set's: raise the heaviest sets (all of them when every set was equal).
      if (target.weight != null && (s.weight == topWeight || s.weight == null)) s.weight = target.weight;
    }
  }

  double? get _topWeight =>
      sets.map((s) => s.weight).whereType<double>().fold<double?>(null, (a, b) => a == null || b > a ? b : a);

  /// "10,8,8" with 3 sets → [10, 8, 8]; "12" with 3 sets → [12, 12, 12]. A shorter list is
  /// padded with its last value; a range ("8-12") keeps its lower bound.
  static List<String> expandReps(String reps, int count) {
    final n = count.clamp(1, maxSets);
    final values =
        westernDigits(reps).split(',').map((r) => _leadingDigits(r.trim())).where((r) => r.isNotEmpty).toList();
    final fill = values.isEmpty ? '' : values.last;
    return [for (var i = 0; i < n; i++) i < values.length ? values[i] : fill];
  }

  /// "١٠،٨" → "10,8": Arabic-Indic digits and the Arabic comma, as an Arabic keyboard types them.
  static String westernDigits(String text) =>
      text.replaceAll('،', ',').replaceAllMapped(RegExp('[٠-٩]'), (m) => '${m[0]!.codeUnitAt(0) - 0x0660}');

  static String _leadingDigits(String text) => RegExp(r'^\d+').stringMatch(text) ?? '';

  int get setCount => sets.length;

  /// Adds or drops sets from the end; a new set repeats the previous one (reps, time and kg).
  set setCount(int value) {
    final count = value.clamp(1, maxSets);
    while (sets.length > count) {
      sets.removeLast();
    }
    while (sets.length < count) {
      sets.add(sets.isEmpty ? SetDraft() : sets.last.copy());
    }
  }

  /// 1-based numbers of the sets still missing their reps (or time).
  List<int> get setsMissing => [
        for (final (i, s) in sets.indexed)
          if (!s.isDone(measure)) i + 1,
      ];

  /// Nothing typed at all: skipped on save instead of blocking it.
  bool get isUntouched => name.trim().isEmpty && sets.every((s) => s.isEmpty) && notes.trim().isEmpty;

  ExerciseRowIssue? get issue {
    if (name.trim().isEmpty) return ExerciseRowIssue.missingName;
    if (setsMissing.isNotEmpty) return ExerciseRowIssue.missingSets;
    return null;
  }

  bool get isValid => issue == null;

  bool get isTimed => measure == ExerciseMeasure.time;

  bool get hasTarget =>
      targetSets != null || targetReps.trim().isNotEmpty || targetWeight != null || targetSeconds != null;

  /// The legacy reps text the API also keeps: "12,10,8", or "12" when every set matched.
  String get summaryReps {
    final values = sets.map((s) => isTimed ? '${s.seconds ?? 0}s' : '${s.repsValue ?? 0}').toList();
    return values.toSet().length == 1 ? values.first : values.join(',');
  }

  Map<String, dynamic> toJson(int orderIndex) => {
        'exerciseName': name.trim(),
        'orderIndex': orderIndex,
        'measure': measure.name,
        'setEntries': [for (final s in sets) s.toJson(measure)],
        'sets': setCount,
        'reps': summaryReps,
        'weight': _topWeight,
        'notes': notes.trim().isEmpty ? null : notes.trim(),
        'target': hasTarget ? _targetJson() : null,
      };

  Map<String, dynamic> _targetJson() => {
        'sets': targetSets,
        'reps': isTimed || targetReps.trim().isEmpty ? null : westernDigits(targetReps.trim()),
        'weight': targetWeight,
        'seconds': isTimed ? targetSeconds : null,
      };
}

/// What picking a training day did to the form.
enum DayPick {
  /// Only the label changed (a new day, or nothing to fill).
  labelled,

  /// The exercises were filled from the last time the client trained that day.
  filled,

  /// The exercises of the previous day the form was filled from were taken away (new day, nothing to fill).
  cleared,

  /// The coach typed their own exercises: ask before replacing them.
  needsConfirm,
}

/// The form as it was, so a day pick or a clear can be undone.
class SessionExercisesSnapshot {
  final String? dayLabel;
  final List<ExerciseRowDraft> rows;
  final String? prefill;

  const SessionExercisesSnapshot._(this.dayLabel, this.rows, this.prefill);
}

/// The session form: a day label and the exercises.
///
/// Picking a day the client trained before fills last time's exercises (a ready-made template).
/// The coach can switch days, clear the day or start empty at any time without leaving the screen:
/// exercises still exactly as filled are swapped freely; once the coach changed them, the screen
/// asks first. Every change can be undone with [restore].
class SessionExercisesDraft {
  String? dayLabel;
  final List<ExerciseRowDraft> rows;

  /// The rows as they were filled from a day, to tell a filled template from the coach's own work.
  String? _prefill;

  SessionExercisesDraft({this.dayLabel, List<ExerciseRowDraft>? rows}) : rows = rows ?? [ExerciseRowDraft()];

  /// Rows from the last session of [label] on the client's board; empty when that day is new.
  static List<ExerciseRowDraft> repeatOf(ExerciseProgressModel? board, String label) {
    final day = board?.days.where((d) => d.label == label).firstOrNull;
    if (board == null || day == null) return const [];
    return board.exercisesFor(day).map(ExerciseRowDraft.fromHistory).toList();
  }

  /// The client's own named days (not the presets) the coach can pick again, most recent first.
  static List<String> pastCustomDays(ExerciseProgressModel? board) {
    final days = [...?board?.days.where((d) => d.label != null && !ExerciseDays.presets.containsKey(d.label))];
    days.sort((a, b) => (b.lastTrained ?? DateTime(0)).compareTo(a.lastTrained ?? DateTime(0)));
    return [for (final d in days) d.label!];
  }

  String _fingerprint() => jsonEncode([for (final (i, r) in rows.indexed) r.toJson(i)]);

  /// Rows still exactly as they were filled from a day.
  bool get isUnchangedPrefill => _prefill != null && _fingerprint() == _prefill;

  /// Nothing of the coach's own would be lost by replacing the rows.
  bool get _nothingToLose => rows.every((r) => r.isUntouched) || isUnchangedPrefill;

  /// True when there is something to clear.
  bool get canClear => !rows.every((r) => r.isUntouched);

  SessionExercisesSnapshot snapshot() => SessionExercisesSnapshot._(dayLabel, [...rows], _prefill);

  void restore(SessionExercisesSnapshot snap) {
    dayLabel = snap.dayLabel;
    _prefill = snap.prefill;
    rows
      ..clear()
      ..addAll(snap.rows);
  }

  /// Picks [label]: fills last time's exercises unless that would replace the coach's own (then
  /// [DayPick.needsConfirm], and nothing changes); [replace] = the coach agreed.
  DayPick pickDay(ExerciseProgressModel? board, String label, {bool replace = false}) {
    final repeat = repeatOf(board, label);
    if (repeat.isNotEmpty && !replace && !_nothingToLose) return DayPick.needsConfirm;
    final wasPrefill = isUnchangedPrefill;
    dayLabel = label;
    if (repeat.isNotEmpty) {
      _setRows(repeat);
      _prefill = _fingerprint();
      return DayPick.filled;
    }
    if (!wasPrefill) return DayPick.labelled;
    startEmpty();
    return DayPick.cleared;
  }

  /// No day: the label goes, and so do exercises still exactly as filled from it. True when rows were cleared.
  bool clearDay() {
    dayLabel = null;
    if (!isUnchangedPrefill) return false;
    startEmpty();
    return true;
  }

  /// One blank exercise, no template: the coach writes their own.
  void startEmpty() {
    _setRows([ExerciseRowDraft()]);
    _prefill = null;
  }

  void _setRows(List<ExerciseRowDraft> next) => rows
    ..clear()
    ..addAll(next);

  List<ExerciseRowDraft> get filled => rows.where((r) => !r.isUntouched).toList();

  /// Save is offered as soon as anything was typed; [firstInvalid] says what still blocks it.
  bool get hasEntries => filled.isNotEmpty;

  ExerciseRowDraft? get firstInvalid => filled.where((r) => !r.isValid).firstOrNull;

  bool get canSave => hasEntries && firstInvalid == null;

  Map<String, dynamic> toJson() => {
        'dayLabel': dayLabel,
        'items': [for (final (i, row) in filled.indexed) row.toJson(i)],
      };
}
