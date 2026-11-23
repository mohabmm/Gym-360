import 'package:gymapp_core/gymapp_core.dart';

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
  String nextTime(String day) => _t('المرة الجاية: $day', 'Next time: $day');
  String get planFinished => _t('خلّصت الخطة كلها! كلّم مدربك على الخطة الجاية.', 'You finished the whole plan! Ask your coach for the next one.');
  String doneOf(int done, int total) => _t('خلّصت $done من $total تمرين', '$done of $total workouts done');
  String thisWeek(int done, int target) => _t('الأسبوع ده: $done من $target', 'This week: $done of $target');
  String get thisWeekLabel => _t('الأسبوع ده', 'This week');
  String get thisMonthLabel => _t('الشهر ده', 'This month');
  String get planDoneLabel => _t('من الخطة', 'Of the plan');
  String exerciseCount(int n) => _t('$n تمارين', n == 1 ? '1 exercise' : '$n exercises');
  String exercisesAndMinutes(int exercises, int minutes) =>
      '${exerciseCount(exercises)} · ${_t('حوالي $minutes دقيقة', 'about $minutes min')}';
  String pending(int n) => _t(n == 1 ? 'تمرين محفوظ على الموبايل – هيتبعت أول ما النت يرجع' : '$n تمارين محفوظة على الموبايل – هتتبعت أول ما النت يرجع',
      n == 1 ? '1 workout saved on this phone – it will upload when you are back online' : '$n workouts saved on this phone – they will upload when you are back online');
  String get offlineCopy => _t('من غير نت – دي آخر نسخة من خطتك', 'Offline – showing the last copy of your plan');
  String get noPlan => _t('مدربك لسه ما بعتلكش خطة', 'Your coach has not sent you a plan yet');
  String get noPlanHint => _t('أول ما يبعتها هتلاقيها هنا وهيجيلك إشعار.', "When they do, it shows up here and you'll get a notification.");
  String get next => _t('التالي', 'Next');
  String get done => _t('تم', 'Done');
  String setLabel(int n) => _t('مجموعة $n', 'Set $n');
  String get kg => _t('كجم', 'kg');
  String get reps => _t('عدّات', 'reps');
  String get seconds => _t('ث', 's');
  String lastTime(String kg) => _t('آخر مرة: $kg كجم', 'Last time: $kg kg');
  String finish(int done, int total) => _t('خلّص التمرين ($done/$total)', 'Finish workout ($done/$total)');
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
  String weightChange(String first, String latest) => _t('$first ← $latest كجم', '$first → $latest kg');
  String best(String kg) => _t('أعلى: $kg كجم', 'Best: $kg kg');
  String get withCoach => _t('مع المدرب', 'With coach');
  String setsDone(int n) => _t('$n مجموعات', n == 1 ? '1 set' : '$n sets');
  String get dayNotFound => _t('اليوم ده مش في خطتك دلوقتي', 'This day is no longer in your plan');
  String get retry => _t('حاول تاني', 'Try again');
  String get loadError => _t('مش قادرين نجيب خطتك دلوقتي', "Couldn't load your plan right now");

  static String kgText(double v) => v == v.roundToDouble() ? v.toInt().toString() : v.toStringAsFixed(1);
}
