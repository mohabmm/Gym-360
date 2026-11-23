import 'package:gymapp_core/gymapp_core.dart';

import '../data/coach_overview.dart';
import 'trainer_plan_strings.dart';

/// Arabic / English copy for "My clients' plans" (who is on track, who needs a nudge).
class ClientPlansStrings extends TrainerPlanStrings {
  const ClientPlansStrings(super.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get clientPlans => _t('خطط عملائي', "My clients' plans");
  String get clientPlansSubtitle => _t('مين ماشي ومين محتاج متابعة', "Who's on track and who needs a nudge");
  String get needsNudge => _t('محتاجين متابعة', 'Needs a nudge');
  String get onTrack => _t('ماشيين في الخطة', 'On their plan');
  String get noPlanYet => _t('من غير خطة', 'No plan yet');
  String get allOnTrack => _t('كل عملائك ماشيين تمام 👏', 'All your clients are on track 👏');
  String get remind => _t('فكّره', 'Remind');
  String get reminded => _t('اتبعتله تذكير ✅', 'Reminder sent ✅');
  String get alreadyReminded => _t('فكّرته النهارده خلاص', 'You already reminded them today');
  String get remindFailed => _t('ما قدرناش نبعت التذكير', "Couldn't send the reminder");
  String get loadFailed => _t('ما قدرناش نجيب خطط العملاء', "Couldn't load your clients' plans");
  String get whatsApp => _t('واتساب', 'WhatsApp');

  String needsNudgeCount(CoachPlanOverview overview) {
    final n = overview.quiet.length;
    return _t('$n عملاء محتاجين متابعة', n == 1 ? '1 client needs a nudge' : '$n clients need a nudge');
  }

  String quietRule(CoachPlanOverview overview) => _t(
      'بيظهر هنا اللي عدّى عليه ${overview.inactiveDays} أيام من غير تمرين',
      'Clients show here after ${overview.inactiveDays} days without a workout');

  String name(UserModel? user) {
    final full = user == null ? '' : '${user.firstName} ${user.lastName}'.trim();
    return full.isEmpty ? client : full;
  }

  String get client => _t('عميل', 'Client');

  /// "This week 1/3 · 4/12 done".
  String progress(CoachClientPlan c) =>
      _t('الأسبوع ده ${c.thisWeek}/${c.weekTarget} · خلّص ${c.doneCount}/${c.total}',
          'This week ${c.thisWeek}/${c.weekTarget} · ${c.doneCount}/${c.total} done');

  /// "No workout for 6 days", "Last workout 2 days ago", "Worked out today", "Plan hasn't started".
  String activity(CoachClientPlan c) {
    if (c.notStarted) return _t('الخطة لسه ما بدأتش', "Plan hasn't started");
    final days = c.daysSince!;
    if (c.isInactive) return _t('من غير تمرين من $days يوم', 'No workout for $days days');
    if (days == 0) return _t('اتمرّن النهارده', 'Worked out today');
    return _t('آخر تمرين من $days يوم', 'Last workout $days days ago');
  }

  /// The WhatsApp message the coach starts from (they can edit it before sending).
  String whatsAppText(UserModel? user, CoachClientPlan c) {
    final first = user?.firstName ?? '';
    final days = c.isInactive ? c.daysSince : null;
    final next = c.nextDay(isArabic);
    final when = days == null ? '' : _t(' بقالك $days يوم من غير تمرين،', " it's been $days days since your last workout,");
    final nextText = next == null ? '' : _t(' تمرينك الجاي: $next.', ' your next workout: $next.');
    return _t('أهلاً $first 👋$when$nextText نكمّل النهارده؟ 💪', 'Hi $first 👋$when$nextText Shall we get back to it today? 💪');
  }
}
