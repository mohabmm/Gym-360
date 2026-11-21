import 'exercise_progress_model.dart';
import 'exercise_progress_strings.dart';
import 'session_insights.dart';
import 'session_summary_model.dart';

/// Arabic-first labels for the "by session" view of the exercise board.
extension SessionStrings on ExerciseProgressStrings {
  String _s(String ar, String en) => isArabic ? ar : en;

  String get tabSessions => _s('الحصص', 'Sessions');
  String get tabExercises => _s('التمارين', 'Exercises');
  String get coachSays => _s('مدربك يقول', 'Your coach says');
  String get effort => _s('المجهود', 'Effort');
  String get sessionDetails => _s('تفاصيل الحصة', 'Session details');
  String get vsLastTime => _s('عن المرة السابقة', 'vs last time');
  String get nextSession => _s('حصتك القادمة', 'Your next session');
  String get focus => _s('التركيز', 'Focus');

  /// "Session 5 · 18/11 · Push Day · Trial"
  String sessionHeading(SessionSummaryModel s) => [
        session(s.sessionNumber),
        date(s.date),
        if (s.dayLabel != null) dayLabel(s.dayLabel),
        if (s.isTrialSession) trial,
      ].join(' · ');

  String bests(int n) => _s('$n رقم قياسي', n == 1 ? '1 new best' : '$n new bests');
  String improvedCount(int n) => _s('$n تمارين تحسنت', '$n improved');
  String targetsHitCount(int n) => _s('حققت $n أهداف', n == 1 ? '1 target hit' : '$n targets hit');
  String exercisesCount(int n) => _s('$n تمارين', n == 1 ? '1 exercise' : '$n exercises');

  /// Folded session: "4 exercises · 1 new best".
  String collapsedLine(SessionRecap recap) => [
        exercisesCount(recap.rows.length),
        if (recap.personalBests > 0) bests(recap.personalBests),
        if (recap.personalBests == 0 && recap.improved > 0) improvedCount(recap.improved),
      ].join(' · ');

  /// "+2.5 kg", "+2 reps", "+10s", "First time", "Same as last time".
  String delta(ExerciseDelta d) {
    if (d.isFirstTime) return _s('أول مرة', 'First time');
    if (d.isSame) return _s('نفس المرة السابقة', 'Same as last time');
    final sign = d.isUp ? '+' : '−';
    if (d.kg != null) return '$sign${kg(d.kg!.abs())}';
    if (d.seconds != null) return '$sign${duration(d.seconds!.abs())}';
    final reps = d.reps!.abs();
    return '$sign$reps ${_s('عدات', reps == 1 ? 'rep' : 'reps')}';
  }

  String targetsSet(int n) => _s('حدد مدربك $n أهداف لك', n == 1 ? 'Your coach set 1 target for you' : 'Your coach set $n targets for you');

  /// "Free session 3 of 3" while the trial still has sessions left; null otherwise.
  String? upcomingFreeSession(ExerciseProgressSummaryModel s) {
    if (!s.hasFreeSessionsLeft) return null;
    final n = s.trialSessionsUsed + 1;
    return _s('الحصة المجانية $n من ${s.trialSessionsTotal}', 'Free session $n of ${s.trialSessionsTotal}');
  }

  /// "1 free session left" / "Free sessions done".
  String freeLeft(ExerciseProgressSummaryModel s) {
    final left = (s.trialSessionsTotal - s.trialSessionsUsed).clamp(0, s.trialSessionsTotal);
    if (left == 0) return _s('انتهت حصصك المجانية', 'Free sessions done');
    if (left == 1) return _s('باقي حصة مجانية واحدة', '1 free session left');
    return _s('باقي $left حصص مجانية', '$left free sessions left');
  }

  /// Why paying matters, once the trial is over.
  String keepProgress(ExerciseProgressModel board) => _s(
        'أرقامك في ${board.exercises.length} تمارين وأهداف مدربك تستمر معك في الباقة.',
        'Your numbers on ${board.exercises.length} exercises and your coach\'s targets carry on with a package.',
      );
}
