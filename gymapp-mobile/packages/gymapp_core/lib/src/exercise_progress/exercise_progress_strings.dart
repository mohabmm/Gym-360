import '../progress_journey/progress_journey_model.dart';
import 'exercise_days.dart';
import 'exercise_insights.dart';
import 'exercise_progress_model.dart';

/// Arabic-first labels for the exercise board (pt.exercise_progress).
class ExerciseProgressStrings {
  final bool isArabic;

  const ExerciseProgressStrings(this.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get title => _t('تقدم التمارين', 'Exercise progress');
  String get allDays => _t('الكل', 'All');
  String get otherDay => _t('أخرى', 'Other');
  String dayLabel(String? label) => label == null ? otherDay : ExerciseDays.display(label, isArabic);
  String get personalBest => _t('رقم قياسي', 'New best');
  String get targetHit => _t('حققت هدف المدرب', 'Hit coach\'s target');
  String get targetMissed => _t('قريب من هدف المدرب', 'Close to coach\'s target');
  String get nextTarget => _t('الهدف القادم', 'Next target');
  String get coachNote => _t('ملاحظة المدرب', 'Coach note');
  String get history => _t('السجل', 'History');
  String get trial => _t('تجريبية', 'Trial');
  String session(int n) => _t('الحصة $n', 'Session $n');
  String get emptyTitle => _t('لا توجد أرقام بعد', 'No numbers yet');
  String get emptyBody => _t(
        'بعد كل حصة يسجل مدربك التمارين والأوزان والعدات، وستظهر هنا مع تقدمك.',
        'After each session your coach records the exercises, weights and reps. They show up here with your progress.',
      );
  String get coachEmptyBody => _t(
        'سجّل تمارين الحصة (المجموعات والعدات والوزن) ليرى العميل أرقامه هنا.',
        'Record the session\'s exercises (sets, reps, weight) and your client sees their numbers here.',
      );
  String get keepTracking => _t(
        'اشترك مع مدربك لتكمل متابعة أرقامك بعد الحصص التجريبية.',
        'Keep training with your coach to keep tracking your numbers after the trial.',
      );

  String trialProgress(ExerciseProgressSummaryModel s) => s.trialSessionsTotal > 0
      ? _t('الحصص التجريبية: ${s.trialSessionsUsed} من ${s.trialSessionsTotal}',
          'Trial sessions: ${s.trialSessionsUsed} of ${s.trialSessionsTotal}')
      : sessionsLogged(s.sessionsLogged);

  String sessionsLogged(int n) => _t('$n حصة مسجلة', '$n sessions logged');

  String goalFocus(ProgressGoalType? goal) => switch (goal) {
        ProgressGoalType.buildMuscle => _t('هدفك بناء العضلات: نتابع وزن التمرين', 'Goal: build muscle — we follow your working weight'),
        ProgressGoalType.loseFat => _t('هدفك حرق الدهون: نتابع إجمالي المجهود', 'Goal: lose fat — we follow the total work you do'),
        ProgressGoalType.generalFitness => _t('هدفك لياقة عامة: نتابع قوتك القصوى المقدرة', 'Goal: general fitness — we follow your estimated max'),
        null => _t('نتابع قوتك القصوى المقدرة في كل تمرين', 'We follow your estimated max on every exercise'),
      };

  String headline(BoardInsight insight) {
    if (insight.isBaseline) {
      return _t('تم تسجيل نقطة البداية — الحصة القادمة تُظهر تقدمك', 'Your starting numbers are in — next session shows your progress');
    }
    final n = insight.improved;
    final m = insight.comparable;
    return switch (insight.lead) {
      ExerciseLead.weight => _t('أصبحت أقوى في $n من $m تمارين', 'Stronger on $n of $m exercises'),
      ExerciseLead.volume => _t('مجهود أكبر في $n من $m تمارين — حرق أكثر', 'More work on $n of $m exercises — more energy burned'),
      ExerciseLead.oneRepMax => _t('قوتك القصوى زادت في $n من $m تمارين', 'Your estimated max went up on $n of $m exercises'),
    };
  }

  String badges(BoardInsight insight) => [
        if (insight.personalBests > 0) _t('${insight.personalBests} أرقام قياسية', '${insight.personalBests} new bests'),
        if (insight.targetsHit > 0) _t('حققت ${insight.targetsHit} أهداف للمدرب', '${insight.targetsHit} coach targets hit'),
      ].join(' · ');

  String leadName(ExerciseLead lead) => switch (lead) {
        ExerciseLead.weight => _t('الوزن', 'Weight'),
        ExerciseLead.volume => _t('إجمالي المجهود', 'Total work'),
        ExerciseLead.oneRepMax => _t('القوة القصوى المقدرة', 'Est. max'),
      };

  /// "60 → 67.5 kg in 6 sessions (+12.5%)"
  String change(LeadChange c) {
    final percent = c.percent;
    final sign = (percent ?? 0) > 0 ? '+' : '';
    final pct = percent == null ? '' : ' ($sign${number(percent)}${_t('٪', '%')})';
    return _t('${kg(c.first)} ← ${kg(c.latest)} في ${c.sessions} حصص$pct',
        '${kg(c.first)} → ${kg(c.latest)} in ${c.sessions} sessions$pct');
  }

  /// "3 × 10,8,8 @ 60 kg"
  String setLine(int sets, String reps, double? weight) =>
      weight == null ? '$sets × $reps' : '$sets × $reps @ ${kg(weight)}';

  String target(ExerciseTargetModel t) => [
        if (t.sets != null || (t.reps?.isNotEmpty ?? false)) '${t.sets ?? '-'} × ${t.reps ?? '-'}',
        if (t.weight != null) kg(t.weight),
      ].join(' @ ');

  String kg(double? value) => value == null ? '-' : '${number(value)} ${_t('كجم', 'kg')}';

  String number(double value) {
    final rounded = (value * 10).round() / 10;
    return rounded == rounded.truncateToDouble() ? rounded.toStringAsFixed(0) : rounded.toStringAsFixed(1);
  }

  String date(DateTime d) => '${d.day}/${d.month}';
}
