/// pt.exercise_progress: backend `ExerciseProgressDto` from `GET /scheduling/me/exercise-progress`
/// and `GET /scheduling/clients/{id}/exercise-progress`.
class ExerciseProgressModel {
  final ExerciseProgressSummaryModel summary;
  final List<TrainingDayModel> days;
  final List<ExerciseHistoryModel> exercises;

  const ExerciseProgressModel({required this.summary, required this.days, required this.exercises});

  factory ExerciseProgressModel.fromJson(Map<String, dynamic> json) => ExerciseProgressModel(
        summary: ExerciseProgressSummaryModel.fromJson(_map(json['summary'])),
        days: _list(json['days'], TrainingDayModel.fromJson),
        exercises: _list(json['exercises'], ExerciseHistoryModel.fromJson),
      );

  bool get isEmpty => exercises.isEmpty;

  /// The day's exercises in the order last trained; every exercise when [day] is null.
  List<ExerciseHistoryModel> exercisesFor(TrainingDayModel? day) {
    if (day == null) return exercises;
    final byKey = {for (final e in exercises) e.key: e};
    return [for (final key in day.exerciseKeys) if (byKey[key] != null) byKey[key]!];
  }
}

class ExerciseProgressSummaryModel {
  final int sessionsLogged;
  final int trialSessionsUsed;
  final int trialSessionsTotal;
  final bool hasPaidPackage;
  final bool isTrialView;
  final DateTime? firstSessionDate;
  final DateTime? lastSessionDate;

  const ExerciseProgressSummaryModel({
    this.sessionsLogged = 0,
    this.trialSessionsUsed = 0,
    this.trialSessionsTotal = 0,
    this.hasPaidPackage = false,
    this.isTrialView = true,
    this.firstSessionDate,
    this.lastSessionDate,
  });

  factory ExerciseProgressSummaryModel.fromJson(Map<String, dynamic> json) => ExerciseProgressSummaryModel(
        sessionsLogged: _int(json['sessionsLogged']),
        trialSessionsUsed: _int(json['trialSessionsUsed']),
        trialSessionsTotal: _int(json['trialSessionsTotal']),
        hasPaidPackage: json['hasPaidPackage'] == true,
        isTrialView: json['isTrialView'] != false,
        firstSessionDate: _date(json['firstSessionDate']),
        lastSessionDate: _date(json['lastSessionDate']),
      );
}

class TrainingDayModel {
  /// The coach's label ("Push Day"); null for sessions without one.
  final String? label;
  final int sessionsCount;
  final DateTime? lastTrained;
  final List<String> exerciseKeys;

  const TrainingDayModel({this.label, this.sessionsCount = 0, this.lastTrained, this.exerciseKeys = const []});

  factory TrainingDayModel.fromJson(Map<String, dynamic> json) => TrainingDayModel(
        label: json['label']?.toString(),
        sessionsCount: _int(json['sessionsCount']),
        lastTrained: _date(json['lastTrained']),
        exerciseKeys: (json['exerciseKeys'] as List? ?? const []).map((e) => e.toString()).toList(),
      );
}

class ExerciseHistoryModel {
  final String key;
  final String name;
  final String? nameAr;
  final List<ExerciseEntryModel> entries;
  final ExerciseTrendModel trend;
  final ExerciseTargetModel? nextTarget;

  const ExerciseHistoryModel({
    required this.key,
    required this.name,
    this.nameAr,
    required this.entries,
    required this.trend,
    this.nextTarget,
  });

  factory ExerciseHistoryModel.fromJson(Map<String, dynamic> json) => ExerciseHistoryModel(
        key: json['key']?.toString() ?? '',
        name: json['name']?.toString() ?? '',
        nameAr: json['nameAr']?.toString(),
        entries: _list(json['entries'], ExerciseEntryModel.fromJson),
        trend: ExerciseTrendModel.fromJson(_map(json['trend'])),
        nextTarget: ExerciseTargetModel.tryFromJson(json['nextTarget']),
      );

  String displayName(bool isArabic) =>
      isArabic && (nameAr?.trim().isNotEmpty ?? false) ? nameAr! : name;

  ExerciseEntryModel? get latest => entries.isEmpty ? null : entries.last;
}

class ExerciseEntryModel {
  final String sessionId;
  final DateTime date;
  final int sessionNumber;
  final bool isTrialSession;
  final String? dayLabel;
  final int sets;
  final String reps;
  final int totalReps;
  final double? weight;
  final double? volume;
  final double? estimatedOneRepMax;
  final String? notes;
  final ExerciseTargetModel? target;

  const ExerciseEntryModel({
    required this.sessionId,
    required this.date,
    required this.sessionNumber,
    this.isTrialSession = false,
    this.dayLabel,
    required this.sets,
    required this.reps,
    this.totalReps = 0,
    this.weight,
    this.volume,
    this.estimatedOneRepMax,
    this.notes,
    this.target,
  });

  factory ExerciseEntryModel.fromJson(Map<String, dynamic> json) => ExerciseEntryModel(
        sessionId: json['sessionId']?.toString() ?? '',
        date: _date(json['date']) ?? DateTime.now(),
        sessionNumber: _int(json['sessionNumber']),
        isTrialSession: json['isTrialSession'] == true,
        dayLabel: json['dayLabel']?.toString(),
        sets: _int(json['sets']),
        reps: json['reps']?.toString() ?? '',
        totalReps: _int(json['totalReps']),
        weight: _double(json['weight']),
        volume: _double(json['volume']),
        estimatedOneRepMax: _double(json['estimatedOneRepMax']),
        notes: json['notes']?.toString(),
        target: ExerciseTargetModel.tryFromJson(json['target']),
      );
}

class ExerciseTrendModel {
  final double? firstWeight;
  final double? latestWeight;
  final double? bestWeight;
  final double? weightChangePercent;
  final double? latestVolume;
  final double? volumeChangePercent;
  final double? latestEstimatedOneRepMax;
  final bool latestIsPersonalBest;

  /// Whether the latest session met the coach's previous target; null when there was none.
  final bool? hitPreviousTarget;

  const ExerciseTrendModel({
    this.firstWeight,
    this.latestWeight,
    this.bestWeight,
    this.weightChangePercent,
    this.latestVolume,
    this.volumeChangePercent,
    this.latestEstimatedOneRepMax,
    this.latestIsPersonalBest = false,
    this.hitPreviousTarget,
  });

  factory ExerciseTrendModel.fromJson(Map<String, dynamic> json) => ExerciseTrendModel(
        firstWeight: _double(json['firstWeight']),
        latestWeight: _double(json['latestWeight']),
        bestWeight: _double(json['bestWeight']),
        weightChangePercent: _double(json['weightChangePercent']),
        latestVolume: _double(json['latestVolume']),
        volumeChangePercent: _double(json['volumeChangePercent']),
        latestEstimatedOneRepMax: _double(json['latestEstimatedOneRepMax']),
        latestIsPersonalBest: json['latestIsPersonalBest'] == true,
        hitPreviousTarget: json['hitPreviousTarget'] as bool?,
      );
}

/// The coach's goal for an exercise next session (any part optional).
class ExerciseTargetModel {
  final int? sets;
  final String? reps;
  final double? weight;

  const ExerciseTargetModel({this.sets, this.reps, this.weight});

  bool get isEmpty => sets == null && (reps?.trim().isEmpty ?? true) && weight == null;

  static ExerciseTargetModel? tryFromJson(Object? json) {
    if (json is! Map) return null;
    final target = ExerciseTargetModel(
      sets: (json['sets'] as num?)?.toInt(),
      reps: json['reps']?.toString(),
      weight: _double(json['weight']),
    );
    return target.isEmpty ? null : target;
  }

  Map<String, dynamic> toJson() => {'sets': sets, 'reps': reps, 'weight': weight};
}

Map<String, dynamic> _map(Object? raw) => raw is Map ? Map<String, dynamic>.from(raw) : <String, dynamic>{};

List<T> _list<T>(Object? raw, T Function(Map<String, dynamic>) parse) =>
    raw is List ? raw.whereType<Map>().map((e) => parse(Map<String, dynamic>.from(e))).toList() : <T>[];

int _int(Object? raw) => (raw as num?)?.toInt() ?? 0;

double? _double(Object? raw) => (raw as num?)?.toDouble();

DateTime? _date(Object? raw) => raw == null ? null : DateTime.tryParse(raw.toString());
