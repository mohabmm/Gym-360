/// pt.training_plan: the plan document (schema v1) and the API's template / plan DTOs.
/// Mirrors `PlanDocument`, `PlanTemplateDto` and `TrainingPlanDto` on the backend.
library;

/// What a plan aims at; `other` carries free text.
enum PlanGoal {
  generalFitness('GeneralFitness'),
  fatLoss('FatLoss'),
  muscleGain('MuscleGain'),
  other('Other');

  final String api;
  const PlanGoal(this.api);

  static PlanGoal parse(String? value) =>
      PlanGoal.values.firstWhere((g) => g.api.toLowerCase() == value?.toLowerCase(), orElse: () => generalFitness);
}

/// One exercise of a workout day: from the library ([exerciseId]) or free text.
/// Either [reps] (e.g. "8-10") or [seconds] (a timed exercise).
class PlanExerciseModel {
  final String key;
  final String? exerciseId;
  final String name;
  final String? nameAr;
  final int sets;
  final String? reps;
  final int? seconds;
  final int? restSeconds;
  final double? targetWeightKg;
  final double? rpe;
  final String? notes;
  final String? videoUrl;

  const PlanExerciseModel({
    this.key = '',
    this.exerciseId,
    required this.name,
    this.nameAr,
    this.sets = 3,
    this.reps,
    this.seconds,
    this.restSeconds,
    this.targetWeightKg,
    this.rpe,
    this.notes,
    this.videoUrl,
  });

  bool get isTimed => seconds != null;

  String displayName(bool isArabic) => isArabic && (nameAr?.isNotEmpty ?? false) ? nameAr! : name;

  factory PlanExerciseModel.fromJson(Map<String, dynamic> json) => PlanExerciseModel(
        key: json['key'] as String? ?? '',
        exerciseId: json['exerciseId'] as String?,
        name: json['name'] as String? ?? '',
        nameAr: json['nameAr'] as String?,
        sets: (json['sets'] as num?)?.toInt() ?? 1,
        reps: json['reps'] as String?,
        seconds: (json['seconds'] as num?)?.toInt(),
        restSeconds: (json['restSeconds'] as num?)?.toInt(),
        targetWeightKg: (json['targetWeightKg'] as num?)?.toDouble(),
        rpe: (json['rpe'] as num?)?.toDouble(),
        notes: json['notes'] as String?,
        videoUrl: json['videoUrl'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'key': key,
        'exerciseId': exerciseId,
        'name': name,
        'nameAr': nameAr,
        'sets': sets,
        'reps': reps,
        'seconds': seconds,
        'restSeconds': restSeconds,
        'targetWeightKg': targetWeightKg,
        'rpe': rpe,
        'notes': notes,
        'videoUrl': videoUrl,
      };
}

/// One workout day, e.g. "Day A – Upper body". [key] stays the same across edits.
class PlanDayModel {
  final String key;
  final String title;
  final String? titleAr;
  final List<PlanExerciseModel> exercises;

  const PlanDayModel({this.key = '', required this.title, this.titleAr, this.exercises = const []});

  String displayTitle(bool isArabic) => isArabic && (titleAr?.isNotEmpty ?? false) ? titleAr! : title;

  factory PlanDayModel.fromJson(Map<String, dynamic> json) => PlanDayModel(
        key: json['key'] as String? ?? '',
        title: json['title'] as String? ?? '',
        titleAr: json['titleAr'] as String?,
        exercises: _list(json['exercises'], PlanExerciseModel.fromJson),
      );

  Map<String, dynamic> toJson() => {
        'key': key,
        'title': title,
        'titleAr': titleAr,
        'exercises': [for (final e in exercises) e.toJson()],
      };
}

class PlanWeekModel {
  final int week;
  final List<PlanDayModel> days;

  const PlanWeekModel({required this.week, this.days = const []});

  factory PlanWeekModel.fromJson(Map<String, dynamic> json) => PlanWeekModel(
        week: (json['week'] as num?)?.toInt() ?? 1,
        days: _list(json['days'], PlanDayModel.fromJson),
      );

  Map<String, dynamic> toJson() => {'week': week, 'days': [for (final d in days) d.toJson()]};
}

/// The body of a plan or template: weeks of workout days, done in sequence (not on fixed dates).
class PlanDocumentModel {
  static const int currentSchemaVersion = 1;

  final int schemaVersion;
  final List<PlanWeekModel> weeks;

  const PlanDocumentModel({this.schemaVersion = currentSchemaVersion, this.weeks = const []});

  /// Every workout day in the order the trainee does them.
  List<PlanDayModel> get sequence => [
        for (final w in [...weeks]..sort((a, b) => a.week.compareTo(b.week))) ...w.days,
      ];

  int get daysPerWeek => weeks.isEmpty ? 0 : weeks.map((w) => w.days.length).reduce((a, b) => a > b ? a : b);

  factory PlanDocumentModel.fromJson(Map<String, dynamic> json) => PlanDocumentModel(
        schemaVersion: (json['schemaVersion'] as num?)?.toInt() ?? currentSchemaVersion,
        weeks: _list(json['weeks'], PlanWeekModel.fromJson),
      );

  Map<String, dynamic> toJson() => {
        'schemaVersion': schemaVersion,
        'weeks': [for (final w in weeks) w.toJson()],
      };
}

/// A template in the picker (no days).
class PlanTemplateSummaryModel {
  final String id;
  final bool isGymTemplate;
  final String name;
  final String? nameAr;
  final PlanGoal goal;
  final String? goalText;
  final int durationWeeks;
  final int daysPerWeek;
  final bool isSample;
  final bool canEdit;

  const PlanTemplateSummaryModel({
    required this.id,
    required this.isGymTemplate,
    required this.name,
    this.nameAr,
    this.goal = PlanGoal.generalFitness,
    this.goalText,
    this.durationWeeks = 4,
    this.daysPerWeek = 3,
    this.isSample = false,
    this.canEdit = false,
  });

  String displayName(bool isArabic) => isArabic && (nameAr?.isNotEmpty ?? false) ? nameAr! : name;

  factory PlanTemplateSummaryModel.fromJson(Map<String, dynamic> json) => PlanTemplateSummaryModel(
        id: json['id'] as String,
        isGymTemplate: (json['scope'] as String?) == 'Gym',
        name: json['name'] as String? ?? '',
        nameAr: json['nameAr'] as String?,
        goal: PlanGoal.parse(json['goal'] as String?),
        goalText: json['goalText'] as String?,
        durationWeeks: (json['durationWeeks'] as num?)?.toInt() ?? 0,
        daysPerWeek: (json['daysPerWeek'] as num?)?.toInt() ?? 0,
        isSample: json['isSample'] as bool? ?? false,
        canEdit: json['canEdit'] as bool? ?? false,
      );
}

class PlanTemplateModel {
  final PlanTemplateSummaryModel summary;
  final PlanDocumentModel document;

  const PlanTemplateModel({required this.summary, required this.document});

  factory PlanTemplateModel.fromJson(Map<String, dynamic> json) => PlanTemplateModel(
        summary: PlanTemplateSummaryModel.fromJson(_map(json['summary'])),
        document: PlanDocumentModel.fromJson(_map(json['document'])),
      );
}

class TrainingPlanSummaryModel {
  final String id;
  final String traineeId;
  final String trainerId;
  final String? sourceTemplateId;
  final String name;
  final String? nameAr;
  final PlanGoal goal;
  final String? goalText;
  final int durationWeeks;
  final int daysPerWeek;
  final int workoutCount;
  final DateTime startDate;
  final String status;
  final int version;
  final DateTime updatedAt;

  const TrainingPlanSummaryModel({
    required this.id,
    required this.traineeId,
    required this.trainerId,
    this.sourceTemplateId,
    required this.name,
    this.nameAr,
    this.goal = PlanGoal.generalFitness,
    this.goalText,
    this.durationWeeks = 0,
    this.daysPerWeek = 0,
    this.workoutCount = 0,
    required this.startDate,
    this.status = 'Active',
    this.version = 1,
    required this.updatedAt,
  });

  bool get isActive => status == 'Active';

  String displayName(bool isArabic) => isArabic && (nameAr?.isNotEmpty ?? false) ? nameAr! : name;

  factory TrainingPlanSummaryModel.fromJson(Map<String, dynamic> json) => TrainingPlanSummaryModel(
        id: json['id'] as String,
        traineeId: json['traineeId'] as String? ?? '',
        trainerId: json['trainerId'] as String? ?? '',
        sourceTemplateId: json['sourceTemplateId'] as String?,
        name: json['name'] as String? ?? '',
        nameAr: json['nameAr'] as String?,
        goal: PlanGoal.parse(json['goal'] as String?),
        goalText: json['goalText'] as String?,
        durationWeeks: (json['durationWeeks'] as num?)?.toInt() ?? 0,
        daysPerWeek: (json['daysPerWeek'] as num?)?.toInt() ?? 0,
        workoutCount: (json['workoutCount'] as num?)?.toInt() ?? 0,
        startDate: DateTime.tryParse(json['startDate'] as String? ?? '') ?? DateTime.now(),
        status: json['status'] as String? ?? 'Active',
        version: (json['version'] as num?)?.toInt() ?? 1,
        updatedAt: DateTime.tryParse(json['updatedAt'] as String? ?? '') ?? DateTime.now(),
      );
}

class TrainingPlanModel {
  final TrainingPlanSummaryModel summary;
  final PlanDocumentModel document;

  const TrainingPlanModel({required this.summary, required this.document});

  factory TrainingPlanModel.fromJson(Map<String, dynamic> json) => TrainingPlanModel(
        summary: TrainingPlanSummaryModel.fromJson(_map(json['summary'])),
        document: PlanDocumentModel.fromJson(_map(json['document'])),
      );
}

/// A trainee's active plan and older ones, for their coach.
class TraineePlansModel {
  final TrainingPlanModel? active;
  final List<TrainingPlanSummaryModel> history;

  const TraineePlansModel({this.active, this.history = const []});

  factory TraineePlansModel.fromJson(Map<String, dynamic> json) => TraineePlansModel(
        active: json['active'] == null ? null : TrainingPlanModel.fromJson(_map(json['active'])),
        history: _list(json['history'], TrainingPlanSummaryModel.fromJson),
      );
}

Map<String, dynamic> _map(dynamic value) => value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};

List<T> _list<T>(dynamic value, T Function(Map<String, dynamic>) parse) =>
    value is List ? [for (final item in value) parse(_map(item))] : <T>[];
