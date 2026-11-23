import 'plan_models.dart';

/// Arabic / English wording shared by the plan screens of every app.
class PlanStrings {
  final bool isArabic;

  const PlanStrings(this.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String goal(PlanGoal goal, [String? text]) => switch (goal) {
        PlanGoal.generalFitness => _t('لياقة عامة', 'General fitness'),
        PlanGoal.fatLoss => _t('حرق دهون', 'Fat loss'),
        PlanGoal.muscleGain => _t('بناء عضل', 'Muscle gain'),
        PlanGoal.other => (text?.isNotEmpty ?? false) ? text! : _t('هدف آخر', 'Other goal'),
      };

  String weeks(int n) => _t('$n أسابيع', n == 1 ? '1 week' : '$n weeks');

  String daysPerWeek(int n) => _t('$n أيام في الأسبوع', n == 1 ? '1 day a week' : '$n days a week');

  String weekLabel(int n) => _t('الأسبوع $n', 'Week $n');

  String sets(int n) => _t('$n مجموعات', n == 1 ? '1 set' : '$n sets');

  /// "3 × 10-12", "3 × 45 ث" — what the exercise asks for.
  String prescription(PlanExerciseModel e) {
    final dose = e.isTimed ? _t('${e.seconds} ث', '${e.seconds}s') : (e.reps ?? '');
    final target = [
      if (e.targetWeightKg != null) _t('${_num(e.targetWeightKg!)} كجم', '${_num(e.targetWeightKg!)} kg'),
      if (e.rpe != null) 'RPE ${_num(e.rpe!)}',
    ].join(' · ');
    return target.isEmpty ? '${e.sets} × $dose' : '${e.sets} × $dose · $target';
  }

  String rest(int seconds) => _t('راحة $seconds ث', 'Rest ${seconds}s');

  static String _num(double v) => v == v.roundToDouble() ? v.toInt().toString() : v.toString();
}
