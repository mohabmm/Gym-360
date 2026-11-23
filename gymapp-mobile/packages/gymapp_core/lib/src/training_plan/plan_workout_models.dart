/// pt.training_plan, trainee side: the next workout, finished workouts and progress.
/// Mirrors `MyPlanDto`, `PlanWorkoutInput` and `PlanProgressDto` on the backend.
library;

import 'dart:math';

import 'plan_models.dart';

/// A workout day at its place in the plan: index 0 = week 1 day 1.
class PlanSlot {
  final int index;
  final int week;
  final PlanDayModel day;

  const PlanSlot({required this.index, required this.week, required this.day});

  /// Every workout day in the order the trainee does them.
  static List<PlanSlot> of(PlanDocumentModel document) {
    final weeks = [...document.weeks]..sort((a, b) => a.week.compareTo(b.week));
    final slots = <PlanSlot>[];
    for (final week in weeks) {
      for (final day in week.days) {
        slots.add(PlanSlot(index: slots.length, week: week.week, day: day));
      }
    }
    return slots;
  }
}

/// Where the trainee is in the plan. Plan days are a sequence, not dates: the next workout
/// is the day after the furthest one done, so a missed day just waits.
class PlanCursorModel {
  final String? nextDayKey;
  final int lastDoneIndex;
  final int total;
  final bool doneToday;
  final List<String> doneKeys;

  const PlanCursorModel({
    this.nextDayKey,
    this.lastDoneIndex = -1,
    this.total = 0,
    this.doneToday = false,
    this.doneKeys = const [],
  });

  bool get finished => nextDayKey == null && total > 0;

  int get doneCount => lastDoneIndex + 1;

  factory PlanCursorModel.fromJson(Map<String, dynamic> json) => PlanCursorModel(
        nextDayKey: json['nextDayKey'] as String?,
        lastDoneIndex: (json['lastDoneIndex'] as num?)?.toInt() ?? -1,
        total: (json['total'] as num?)?.toInt() ?? 0,
        doneToday: json['doneToday'] as bool? ?? false,
        doneKeys: [for (final k in (json['doneKeys'] as List? ?? const [])) k as String],
      );

  /// The server's cursor moved on by workouts finished on this phone that are not uploaded yet
  /// (same rule as the server, so the card is right offline too).
  PlanCursorModel withPending(PlanDocumentModel document, Iterable<String> pendingDayKeys, {bool pendingToday = false}) {
    final slots = PlanSlot.of(document);
    var last = lastDoneIndex;
    final keys = {...doneKeys};
    for (final key in pendingDayKeys) {
      final slot = slots.where((s) => s.day.key == key).firstOrNull;
      if (slot == null) continue;
      keys.add(key);
      if (slot.index > last) last = slot.index;
    }
    final next = last + 1 < slots.length ? slots[last + 1].day.key : null;
    return PlanCursorModel(
      nextDayKey: next,
      lastDoneIndex: last,
      total: slots.length,
      doneToday: doneToday || pendingToday,
      doneKeys: keys.toList(),
    );
  }
}

/// Workouts this week (Saturday to Friday) and this month.
class PlanWeekStatsModel {
  final int thisWeek;
  final int weekTarget;
  final int thisMonth;
  final DateTime? lastWorkoutOn;

  const PlanWeekStatsModel({this.thisWeek = 0, this.weekTarget = 0, this.thisMonth = 0, this.lastWorkoutOn});

  factory PlanWeekStatsModel.fromJson(Map<String, dynamic> json) => PlanWeekStatsModel(
        thisWeek: (json['thisWeek'] as num?)?.toInt() ?? 0,
        weekTarget: (json['weekTarget'] as num?)?.toInt() ?? 0,
        thisMonth: (json['thisMonth'] as num?)?.toInt() ?? 0,
        lastWorkoutOn: DateTime.tryParse(json['lastWorkoutOn'] as String? ?? ''),
      );

  /// Counts with workouts finished on this phone but not uploaded yet.
  PlanWeekStatsModel plus({int week = 0, int month = 0}) =>
      PlanWeekStatsModel(thisWeek: thisWeek + week, weekTarget: weekTarget, thisMonth: thisMonth + month, lastWorkoutOn: lastWorkoutOn);

  /// In Egypt the week starts on Saturday.
  static DateTime weekStart(DateTime day) {
    final date = DateTime(day.year, day.month, day.day);
    return date.subtract(Duration(days: (date.weekday - DateTime.saturday + 7) % 7));
  }
}

/// `GET training/me/plan`: the plan, the next day, this week, and last weights by exercise key.
class MyPlanModel {
  final TrainingPlanModel plan;
  final PlanCursorModel cursor;
  final PlanWeekStatsModel stats;
  final Map<String, double> lastWeights;

  const MyPlanModel({required this.plan, required this.cursor, required this.stats, this.lastWeights = const {}});

  factory MyPlanModel.fromJson(Map<String, dynamic> json) => MyPlanModel(
        plan: TrainingPlanModel.fromJson(_map(json['plan'])),
        cursor: PlanCursorModel.fromJson(_map(json['cursor'])),
        stats: PlanWeekStatsModel.fromJson(_map(json['stats'])),
        lastWeights: {
          for (final e in _map(json['lastWeights']).entries) e.key: (e.value as num).toDouble(),
        },
      );
}

/// One set as the trainee did it (or skipped it).
class CompletedSetModel {
  final String exerciseKey;
  final String? exerciseId;
  final String exerciseName;
  final String? exerciseNameAr;
  final int setNumber;
  final bool done;
  final int? reps;
  final double? weightKg;
  final int? seconds;

  const CompletedSetModel({
    required this.exerciseKey,
    this.exerciseId,
    required this.exerciseName,
    this.exerciseNameAr,
    required this.setNumber,
    this.done = false,
    this.reps,
    this.weightKg,
    this.seconds,
  });

  CompletedSetModel copyWith({bool? done, int? reps, double? weightKg, int? seconds}) => CompletedSetModel(
        exerciseKey: exerciseKey,
        exerciseId: exerciseId,
        exerciseName: exerciseName,
        exerciseNameAr: exerciseNameAr,
        setNumber: setNumber,
        done: done ?? this.done,
        reps: reps ?? this.reps,
        weightKg: weightKg ?? this.weightKg,
        seconds: seconds ?? this.seconds,
      );

  Map<String, dynamic> toJson() => {
        'exerciseKey': exerciseKey,
        'exerciseId': exerciseId,
        'exerciseName': exerciseName,
        'exerciseNameAr': exerciseNameAr,
        'setNumber': setNumber,
        'done': done,
        'reps': reps,
        'weightKg': weightKg,
        'seconds': seconds,
      };
}

/// A finished workout in the history.
class PlanWorkoutModel {
  final String id;
  final String clientId;
  final String dayKey;
  final String dayTitle;
  final String? dayTitleAr;
  final int week;
  final DateTime completedOn;
  final int setsDone;
  final String source;
  final bool isVoided;

  const PlanWorkoutModel({
    required this.id,
    required this.clientId,
    required this.dayKey,
    required this.dayTitle,
    this.dayTitleAr,
    this.week = 1,
    required this.completedOn,
    this.setsDone = 0,
    this.source = 'App',
    this.isVoided = false,
  });

  bool get fromCoachSession => source == 'CoachSession';

  String displayTitle(bool isArabic) => isArabic && (dayTitleAr?.isNotEmpty ?? false) ? dayTitleAr! : dayTitle;

  factory PlanWorkoutModel.fromJson(Map<String, dynamic> json) => PlanWorkoutModel(
        id: json['id'] as String? ?? '',
        clientId: json['clientId'] as String? ?? '',
        dayKey: json['dayKey'] as String? ?? '',
        dayTitle: json['dayTitle'] as String? ?? '',
        dayTitleAr: json['dayTitleAr'] as String?,
        week: (json['week'] as num?)?.toInt() ?? 1,
        completedOn: DateTime.tryParse(json['completedOn'] as String? ?? '') ?? DateTime.now(),
        setsDone: (json['setsDone'] as num?)?.toInt() ?? 0,
        source: json['source'] as String? ?? 'App',
        isVoided: json['isVoided'] as bool? ?? false,
      );
}

class PlanWeightPointModel {
  final DateTime on;
  final double weightKg;

  const PlanWeightPointModel({required this.on, required this.weightKg});

  factory PlanWeightPointModel.fromJson(Map<String, dynamic> json) => PlanWeightPointModel(
        on: DateTime.tryParse(json['on'] as String? ?? '') ?? DateTime.now(),
        weightKg: (json['weightKg'] as num?)?.toDouble() ?? 0,
      );
}

/// Heaviest weight per workout for one exercise, oldest first.
class PlanExerciseTrendModel {
  final String identity;
  final String name;
  final String? nameAr;
  final List<PlanWeightPointModel> points;

  const PlanExerciseTrendModel({required this.identity, required this.name, this.nameAr, this.points = const []});

  String displayName(bool isArabic) => isArabic && (nameAr?.isNotEmpty ?? false) ? nameAr! : name;

  double? get first => points.isEmpty ? null : points.first.weightKg;
  double? get latest => points.isEmpty ? null : points.last.weightKg;
  double? get best => points.isEmpty ? null : points.map((p) => p.weightKg).reduce((a, b) => a > b ? a : b);

  factory PlanExerciseTrendModel.fromJson(Map<String, dynamic> json) => PlanExerciseTrendModel(
        identity: json['identity'] as String? ?? '',
        name: json['name'] as String? ?? '',
        nameAr: json['nameAr'] as String?,
        points: _list(json['points'], PlanWeightPointModel.fromJson),
      );
}

/// `GET training/me/plan-progress`.
class PlanProgressModel {
  final String? planId;
  final PlanCursorModel? cursor;
  final PlanWeekStatsModel stats;
  final List<PlanWorkoutModel> history;
  final List<PlanExerciseTrendModel> exercises;

  const PlanProgressModel({
    this.planId,
    this.cursor,
    this.stats = const PlanWeekStatsModel(),
    this.history = const [],
    this.exercises = const [],
  });

  factory PlanProgressModel.fromJson(Map<String, dynamic> json) => PlanProgressModel(
        planId: json['planId'] as String?,
        cursor: json['cursor'] == null ? null : PlanCursorModel.fromJson(_map(json['cursor'])),
        stats: PlanWeekStatsModel.fromJson(_map(json['stats'])),
        history: _list(json['history'], PlanWorkoutModel.fromJson),
        exercises: _list(json['exercises'], PlanExerciseTrendModel.fromJson),
      );
}

/// What the server did with an upload.
enum PlanWorkoutOutcome {
  created('created'),
  alreadySynced('already_synced'),
  duplicate('duplicate'),
  planChanged('plan_changed'),
  planReplaced('plan_replaced');

  final String api;
  const PlanWorkoutOutcome(this.api);

  static PlanWorkoutOutcome parse(String? value) =>
      PlanWorkoutOutcome.values.firstWhere((o) => o.api == value, orElse: () => created);
}

/// A random (v4) id made on the phone when a workout is finished: the upload's idempotency key.
String newClientId([Random? random]) {
  final rng = random ?? Random.secure();
  final bytes = List<int>.generate(16, (_) => rng.nextInt(256));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
  return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
}

Map<String, dynamic> _map(dynamic value) => value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};

List<T> _list<T>(dynamic value, T Function(Map<String, dynamic>) parse) =>
    value is List ? [for (final item in value) parse(_map(item))] : <T>[];
