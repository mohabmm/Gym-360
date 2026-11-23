import 'package:gymapp_core/gymapp_core.dart';

import '../data/my_plan_view.dart';
import '../data/workout_session.dart';

/// Arabic / English copy for the trainee's plan screens (Egyptian Arabic, like the rest of the app).
class TraineePlanStrings extends PlanStrings {
  const TraineePlanStrings(super.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get todaysWorkout => _t('تمرين النهارده', "Today's workout");
  String get myPlan => _t('خطتي', 'My plan');
  String get planTab => _t('الخطة', 'Plan');
  String get progressTab => _t('تقدمي', 'Progress');
  String get start => _t('ابدأ التمرين', 'Start workout');
  String get doneToday => _t('خلّصت تمرين النهارده 💪', "Today's workout is done 💪");
  /// "Week 1 · Day A – Upper body".
  String slotTitle(PlanSlot slot) => '${weekLabel(slot.week)} · ${slot.day.displayTitle(isArabic)}';
  String nextTime(PlanSlot slot) => _t('المرة الجاية: ${slotTitle(slot)}', 'Next time: ${slotTitle(slot)}');
  String get planFinished => _t('خلّصت الخطة كلها! كلّم مدربك على الخطة الجاية.', 'You finished the whole plan! Ask your coach for the next one.');
  String doneOf(PlanCursorModel c) => _t('خلّصت ${c.doneCount} من ${c.total} تمرين', '${c.doneCount} of ${c.total} workouts done');
  String thisWeek(PlanWeekStatsModel w) => _t('الأسبوع ده: ${w.thisWeek} من ${w.weekTarget}', 'This week: ${w.thisWeek} of ${w.weekTarget}');
  String get thisWeekLabel => _t('الأسبوع ده', 'This week');
  String get thisMonthLabel => _t('الشهر ده', 'This month');
  String get planDoneLabel => _t('من الخطة', 'Of the plan');
  String exerciseCount(PlanDayModel day) {
    final n = day.exercises.length;
    return _t('$n تمارين', n == 1 ? '1 exercise' : '$n exercises');
  }
  String exercisesAndMinutes(PlanDayModel day) {
    final minutes = WorkoutSession.estimatedMinutes(day);
    return '${exerciseCount(day)} · ${_t('حوالي $minutes دقيقة', 'about $minutes min')}';
  }
  String pending(MyPlanView view) {
    final n = view.pendingCount;
    return _t(n == 1 ? 'تمرين محفوظ على الموبايل – هيتبعت أول ما النت يرجع' : '$n تمارين محفوظة على الموبايل – هتتبعت أول ما النت يرجع',
      n == 1 ? '1 workout saved on this phone – it will upload when you are back online' : '$n workouts saved on this phone – they will upload when you are back online');
  }
  String get offlineCopy => _t('من غير نت – دي آخر نسخة من خطتك', 'Offline – showing the last copy of your plan');
  String get noPlan => _t('مدربك لسه ما بعتلكش خطة', 'Your coach has not sent you a plan yet');
  String get noPlanHint => _t('أول ما يبعتها هتلاقيها هنا وهيجيلك إشعار.', "When they do, it shows up here and you'll get a notification.");
  String get next => _t('التالي', 'Next');
  String get done => _t('تم', 'Done');
  String setLabel(CompletedSetModel set) => _t('مجموعة ${set.setNumber}', 'Set ${set.setNumber}');
  String get kg => _t('كجم', 'kg');
  String get reps => _t('عدّات', 'reps');
  String get seconds => _t('ث', 's');
  String lastTime(double kg) => _t('آخر مرة: ${kgText(kg)} كجم', 'Last time: ${kgText(kg)} kg');
  String finish(WorkoutSession w) => _t('خلّص التمرين (${w.doneCount}/${w.totalCount})', 'Finish workout (${w.doneCount}/${w.totalCount})');
  String get tickASet => _t('علّم على مجموعة واحدة على الأقل', 'Tick at least one set');
  String get savedOnline => _t('تمام! التمرين اتسجل 💪', 'Nice! Workout saved 💪');
  String get savedOffline => _t('التمرين اتحفظ على الموبايل وهيتبعت أول ما النت يرجع', 'Saved on this phone – it will upload when you are back online');
  String get notePlanChanged => _t('اتسجل، بس المدرب غيّر الخطة من ساعتها', 'Saved – your coach changed the plan since');
  String get notePlanReplaced => _t('اتسجل على خطتك القديمة – مدربك بعتلك خطة جديدة', 'Saved on your old plan – your coach sent a new one');
  String get noteDuplicate => _t('اليوم ده كان متسجل النهارده خلاص', 'This day was already logged today');
  String get leaveTitle => _t('تسيب التمرين؟', 'Leave the workout?');
  String get leaveBody => _t('المجموعات اللي علّمت عليها مش هتتحفظ.', "The sets you ticked won't be saved.");
  String get stay => _t('كمّل', 'Stay');
  String get leave => _t('اخرج', 'Leave');
  String get history => _t('التمارين اللي خلصتها', 'Workouts done');
  String get noHistory => _t('لسه ما خلصتش تمرين من الخطة', 'No plan workouts done yet');
  String get weights => _t('الأوزان', 'Weights');
  String get noWeights => _t('سجّل الأوزان في تمرينك وهتشوف تقدمك هنا', 'Log your weights and your progress shows here');
  String weightChange(PlanExerciseTrendModel t) {
    final first = kgText(t.first ?? 0);
    final latest = kgText(t.latest ?? 0);
    return _t('$first ← $latest كجم', '$first → $latest kg');
  }
  String best(PlanExerciseTrendModel t) => _t('أعلى: ${kgText(t.best ?? 0)} كجم', 'Best: ${kgText(t.best ?? 0)} kg');
  String get withCoach => _t('مع المدرب', 'With coach');
  String setsDone(PlanWorkoutModel w) {
    final n = w.setsDone;
    return _t('$n مجموعات', n == 1 ? '1 set' : '$n sets');
  }
  String get dayNotFound => _t('اليوم ده مش في خطتك دلوقتي', 'This day is no longer in your plan');
  String get retry => _t('حاول تاني', 'Try again');
  String get loadError => _t('مش قادرين نجيب خطتك دلوقتي', "Couldn't load your plan right now");

  // ---- First-month challenge (mem.first_month_challenge)
  String get challengeTitle => _t('تحدي الشهر الأول', 'First-month challenge');
  String challengeCount(ChallengeStatusModel c) => _t('${c.count} / ${c.target} تمرين', '${c.count} / ${c.target} workouts');
  String challengeLeft(ChallengeStatusModel c) {
    final left = (c.target - c.count).clamp(0, c.target);
    final days = c.daysLeft;
    return _t('فاضل $left ${left == 1 ? 'تمرين' : 'تمارين'} · $days يوم باقيين',
        '$left ${left == 1 ? 'workout' : 'workouts'} to go · $days ${days == 1 ? 'day' : 'days'} left');
  }
  String challengeReward(ChallengeStatusModel c) => _t('المكافأة: ${c.reward(true)}', 'Reward: ${c.reward(false)}');
  String get challengeWon => _t('مبروك! كسبت التحدي 🎉', 'You won the challenge 🎉');
  String get challengeClaim => _t('كلّم الاستقبال أو الكابتن عشان تاخد مكافأتك.', 'Ask the desk or your coach for your reward.');

  static String kgText(double v) => v == v.roundToDouble() ? v.toInt().toString() : v.toStringAsFixed(1);
}
