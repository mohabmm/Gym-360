import 'package:gymapp_core/gymapp_core.dart';

/// One exercise while the coach edits it.
class ExerciseDraft {
  String key;
  String? exerciseId;
  String name;
  String? nameAr;
  int sets;
  String reps;
  int? seconds;
  int? restSeconds;
  double? targetWeightKg;
  double? rpe;
  String notes;
  String videoUrl;

  /// The library's demo animation, shown while editing; never sent (the server resolves it).
  String? demoMediaUrl;

  ExerciseDraft({
    this.key = '',
    this.exerciseId,
    this.name = '',
    this.nameAr,
    this.sets = 3,
    this.reps = '10',
    this.seconds,
    this.restSeconds = 60,
    this.targetWeightKg,
    this.rpe,
    this.notes = '',
    this.videoUrl = '',
  });

  factory ExerciseDraft.from(PlanExerciseModel e) => ExerciseDraft(
        key: e.key,
        exerciseId: e.exerciseId,
        name: e.name,
        nameAr: e.nameAr,
        sets: e.sets,
        reps: e.reps ?? '',
        seconds: e.seconds,
        restSeconds: e.restSeconds,
        targetWeightKg: e.targetWeightKg,
        rpe: e.rpe,
        notes: e.notes ?? '',
        videoUrl: e.videoUrl ?? '',
      );

  bool get isTimed => seconds != null;

  bool get isValid => name.trim().isNotEmpty && sets >= 1 && (isTimed ? seconds! >= 1 : reps.trim().isNotEmpty);

  /// A copy for another week: same content, a fresh key from the server.
  ExerciseDraft copy() => ExerciseDraft.from(toModel())..key = '';

  PlanExerciseModel toModel() => PlanExerciseModel(
        key: key,
        exerciseId: exerciseId,
        name: name.trim(),
        nameAr: nameAr,
        sets: sets,
        reps: isTimed ? null : reps.trim(),
        seconds: seconds,
        restSeconds: restSeconds,
        targetWeightKg: targetWeightKg,
        rpe: rpe,
        notes: _blank(notes),
        videoUrl: _blank(videoUrl),
      );
}

/// One workout day while the coach edits it.
class DayDraft {
  String key;
  String title;
  String? titleAr;
  final List<ExerciseDraft> exercises;

  DayDraft({this.key = '', this.title = '', this.titleAr, List<ExerciseDraft>? exercises})
      : exercises = exercises ?? [];

  factory DayDraft.from(PlanDayModel d) => DayDraft(
        key: d.key,
        title: d.title,
        titleAr: d.titleAr,
        exercises: [for (final e in d.exercises) ExerciseDraft.from(e)],
      );

  bool get isValid => title.trim().isNotEmpty && exercises.isNotEmpty && exercises.every((e) => e.isValid);

  DayDraft copy() => DayDraft(title: title, titleAr: titleAr, exercises: [for (final e in exercises) e.copy()]);

  /// Moves the exercise at [from] so it ends up at index [to].
  void moveExercise(int from, int to) => exercises.insert(to, exercises.removeAt(from));

  PlanDayModel toModel() => PlanDayModel(
        key: key,
        title: title.trim(),
        titleAr: _blank(titleAr ?? ''),
        exercises: [for (final e in exercises) e.toModel()],
      );
}

/// The plan the builder edits. With [sameEveryWeek] the coach edits week 1 only and the server
/// repeats it for [durationWeeks] weeks ("copy week 1 to all weeks"); otherwise every week is
/// edited on its own. Day and exercise keys are kept, so a trainee's finished workouts still
/// match their days after an edit.
class PlanDraft {
  static const int defaultWeeks = 4;
  static const int maxWeeks = 52;
  static const int maxDaysPerWeek = 7;


  String name;
  String? nameAr;
  PlanGoal goal;
  String goalText;
  int durationWeeks;
  bool sameEveryWeek;
  final List<List<DayDraft>> weeks;

  /// pt.starter_plan: a 3-day plan or a one-week program is held to one week.
  PlanLimit limit = PlanLimit.none;

  PlanDraft({
    this.name = '',
    this.nameAr,
    this.goal = PlanGoal.generalFitness,
    this.goalText = '',
    this.durationWeeks = defaultWeeks,
    this.sameEveryWeek = true,
    List<List<DayDraft>>? weeks,
  }) : weeks = weeks ?? [[]];

  /// A plan built from scratch: one empty day, 4 weeks, same every week.
  factory PlanDraft.blank() => PlanDraft(weeks: [
        [DayDraft()]
      ]);

  /// From a template or an existing plan. Identical weeks collapse into "same every week".
  factory PlanDraft.fromDocument(PlanDocumentModel doc, {required String name, String? nameAr, PlanGoal goal = PlanGoal.generalFitness, String? goalText}) {
    final weeks = [for (final w in [...doc.weeks]..sort((a, b) => a.week.compareTo(b.week))) w.days];
    final same = weeks.length <= 1 || weeks.every((w) => _sameShape(w, weeks.first));
    return PlanDraft(
      name: name,
      nameAr: nameAr,
      goal: goal,
      goalText: goalText ?? '',
      durationWeeks: weeks.isEmpty ? defaultWeeks : weeks.length,
      sameEveryWeek: same,
      weeks: [
        for (final days in same ? weeks.take(1) : weeks) [for (final d in days) DayDraft.from(d)]
      ],
    );
  }

  /// Starting from a template: a copy of its name, goal and days.
  factory PlanDraft.fromTemplate(PlanTemplateModel t) => PlanDraft.fromDocument(t.document,
      name: t.summary.name, nameAr: t.summary.nameAr, goal: t.summary.goal, goalText: t.summary.goalText);

  /// Starting from a ready program: a copy of its name, goal and days.
  factory PlanDraft.fromProgram(PlanProgramModel p) => PlanDraft.fromDocument(p.document,
      name: p.summary.name, nameAr: p.summary.nameAr, goal: p.summary.goal);

  /// Editing a client's plan.
  factory PlanDraft.fromPlan(TrainingPlanModel p) => PlanDraft.fromDocument(p.document,
      name: p.summary.name, nameAr: p.summary.nameAr, goal: p.summary.goal, goalText: p.summary.goalText);

  List<DayDraft> get firstWeek => weeks.first;

  int get workoutCount => sameEveryWeek ? firstWeek.length * durationWeeks : weeks.fold(0, (n, w) => n + w.length);

  /// Workout days a week the builder allows.
  int get maxDays => limit.maxDaysPerWeek;

  bool get isValid =>
      name.trim().isNotEmpty &&
      (goal != PlanGoal.other || goalText.trim().isNotEmpty) &&
      durationWeeks <= limit.maxWeeks &&
      weeks.length <= limit.maxWeeks &&
      weeks.every((w) => w.isNotEmpty && w.length <= maxDays && w.every((d) => d.isValid));

  /// Holds this draft to [to]: at most [PlanLimit.maxWeeks] weeks of [PlanLimit.maxDaysPerWeek] days
  /// (a one-week limit keeps week 1 only; no change for none).
  void limitTo(PlanLimit to) {
    limit = to;
    if (to == PlanLimit.none) return;
    if (to.maxWeeks == 1) copyWeekOneToAll();
    setDurationWeeks(durationWeeks);
    for (final week in weeks) {
      if (week.length > to.maxDaysPerWeek) week.removeRange(to.maxDaysPerWeek, week.length);
    }
  }

  /// Days the coach left unnamed get "Day 1", "Day 2", … (Arabic: «اليوم ١») so a plan built
  /// from scratch saves without naming every day; the hint the field shows is never saved.
  void nameUnnamedDays({required bool isArabic}) {
    for (final week in weeks) {
      for (var i = 0; i < week.length; i++) {
        final day = week[i];
        if (day.title.trim().isNotEmpty) continue;
        final arabic = 'اليوم ${_arabicDigits(i + 1)}';
        day.title = isArabic && (day.titleAr?.trim().isNotEmpty ?? false) ? day.titleAr!.trim() : 'Day ${i + 1}';
        if (isArabic && (day.titleAr?.trim().isEmpty ?? true)) day.titleAr = arabic;
      }
    }
  }

  static String _arabicDigits(int n) => n.toString().split('').map((d) => '٠١٢٣٤٥٦٧٨٩'[int.parse(d)]).join();

  /// Edit every week on its own, starting from copies of week 1.
  void splitWeeks() {
    if (!sameEveryWeek) return;
    sameEveryWeek = false;
    weeks
      ..removeRange(1, weeks.length)
      ..addAll([
        for (var i = 1; i < durationWeeks; i++) [for (final d in firstWeek) d.copy()]
      ]);
  }

  /// "Copy week 1 to all weeks": the other weeks become copies of week 1 again.
  void copyWeekOneToAll() {
    sameEveryWeek = true;
    weeks.removeRange(1, weeks.length);
  }

  void setDurationWeeks(int value) {
    durationWeeks = value.clamp(1, limit.maxWeeks);
    if (sameEveryWeek) return;
    while (weeks.length > durationWeeks) {
      weeks.removeLast();
    }
    while (weeks.length < durationWeeks) {
      weeks.add([for (final d in weeks.last) d.copy()]);
    }
  }

  /// The body of POST/PUT plan and template requests (`PlanContentInput`).
  Map<String, dynamic> toContentJson() => {
        'name': name.trim(),
        'nameAr': _blank(nameAr ?? ''),
        'goal': goal.api,
        'goalText': goal == PlanGoal.other ? goalText.trim() : null,
        'repeatWeekOneForWeeks': sameEveryWeek ? durationWeeks : null,
        'document': PlanDocumentModel(weeks: [
          for (var i = 0; i < weeks.length; i++)
            PlanWeekModel(week: i + 1, days: [for (final d in weeks[i]) d.toModel()])
        ]).toJson(),
      };

  static bool _sameShape(List<PlanDayModel> a, List<PlanDayModel> b) {
    String shape(List<PlanDayModel> days) => [
          for (final d in days) [d.title, for (final e in d.exercises) '${e.name}|${e.sets}|${e.reps}|${e.seconds}|${e.targetWeightKg}'].join(',')
        ].join(';');
    return shape(a) == shape(b);
  }
}

String? _blank(String value) => value.trim().isEmpty ? null : value.trim();

/// pt.starter_plan: `Starter` — the first 3-day plan (no template); `OneWeek` / `ThirtyDays` —
/// from a template or ready program after a staff unlock of that length; `None` — no limit (flag off).
enum PlanLimit {
  none(lengthDays: 0, maxWeeks: PlanDraft.maxWeeks, maxDaysPerWeek: PlanDraft.maxDaysPerWeek),
  starter(lengthDays: 3, maxWeeks: 1, maxDaysPerWeek: 3),
  oneWeek(lengthDays: 7, maxWeeks: 1, maxDaysPerWeek: PlanDraft.maxDaysPerWeek),
  thirtyDays(lengthDays: 30, maxWeeks: 4, maxDaysPerWeek: PlanDraft.maxDaysPerWeek);

  /// Calendar days the plan lasts (0 = never ends by date).
  final int lengthDays;
  final int maxWeeks;
  final int maxDaysPerWeek;

  const PlanLimit({required this.lengthDays, required this.maxWeeks, required this.maxDaysPerWeek});

  /// The staff unlocked a program (one week or 30 days) that the coach may now give.
  bool get isUnlocked => this == oneWeek || this == thirtyDays;

  static PlanLimit parse(String? api) => switch (api) {
        'Starter' => PlanLimit.starter,
        'OneWeek' => PlanLimit.oneWeek,
        'ThirtyDays' => PlanLimit.thirtyDays,
        _ => PlanLimit.none,
      };
}
