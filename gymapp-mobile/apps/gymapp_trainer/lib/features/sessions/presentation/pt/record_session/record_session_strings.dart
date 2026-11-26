import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import 'session_time_draft.dart';

/// Arabic / English copy for recording a PT session.
class RecordSessionStrings {
  final bool isArabic;

  const RecordSessionStrings(this.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get title => _t('تسجيل حصة', 'Record session');
  String get when => _t('متى الحصة؟', 'When is the session?');
  String get today => _t('اليوم', 'Today');
  String get yesterday => _t('أمس', 'Yesterday');
  String get otherDay => _t('يوم آخر', 'Other day');
  String get startTime => _t('وقت البداية', 'Start time');
  String get now => _t('الآن', 'Now');
  String get duration => _t('مدة الحصة', 'Duration');
  String get note => _t('ملاحظة (اختياري)', 'Note (optional)');
  String get noteHint => _t('مثال: ركزنا على الظهر', 'e.g. focused on back');
  String get record => _t('تسجيل الحصة · تُخصم حصة واحدة', 'Record session · uses 1 session');
  String get client => _t('عميل', 'Client');
  String get done => _t('تم', 'Done');
  String get recordExercises => _t('سجّل التمارين', 'Record exercises');
  String get recordMeasurements => _t('سجّل القياسات', 'Record measurements');
  String get recordedTitle => _t('تم تسجيل الحصة ✅', 'Session recorded ✅');
  String get alreadyTitle => _t('هذه الحصة مسجلة بالفعل', 'This session is already recorded');
  String get alreadyBody =>
      _t('لم يتم خصم حصة جديدة.', 'No extra session was used.');
  String get saveFailed => _t('تعذّر تسجيل الحصة، حاول مرة أخرى', 'Could not record the session, please try again');
  String get noSessionsLeft => _t('لا توجد حصص متبقية في الباقة', 'No sessions left in the package');
  String get notYourClient => _t('هذا العميل ليس لديه باقة معك', 'This client has no package with you');

  String get planDay => _t('يوم من الخطة (اختياري)', 'Plan day (optional)');
  String get noPlanDay => _t('بدون', 'None');
  String get planDayHint => _t('يتحسب للعميل تمرين من خطته', 'Counts as this workout of the client\'s plan');
  String get nextPlanDay => _t('التالي', 'Next');

  String minutes(int m) => _t('$m د', '$m min');

  String leftAfter(int left, int total) => _t(
        'متبقي $left من $total ← ${left - 1} بعد هذه الحصة',
        '$left of $total left → ${left - 1} after this',
      );

  String left(int n) => _t('متبقي $n حصة', n == 1 ? '1 session left' : '$n sessions left');

  String willConfirm(String name) => name.isEmpty
      ? _t('سيُطلب من العميل تأكيد الحصة.', 'The client will be asked to confirm.')
      : _t('سيُطلب من $name تأكيد الحصة.', '$name will be asked to confirm.');

  String issue(SessionTimeIssue issue) => switch (issue) {
        SessionTimeIssue.tooOld => _t(
            'يمكن تسجيل حصة حتى ${SessionTimeDraft.maxDaysBack} أيام بعد موعدها. للأقدم تواصل مع الاستقبال.',
            'Sessions can be recorded up to ${SessionTimeDraft.maxDaysBack} days later. Ask the desk for older ones.'),
        SessionTimeIssue.notStarted => _t('هذه الحصة لم تبدأ بعد — اختر وقت بداية الآن أو قبله.',
            "This session hasn't started yet — pick a start time now or earlier."),
      };

  String day(DateTime d, SessionTimeDraft draft) {
    if (DateUtils.isSameDay(d, draft.today)) return today;
    if (DateUtils.isSameDay(d, draft.yesterday)) return yesterday;
    return DateFormat('EEE d MMM', isArabic ? 'ar' : 'en').format(d);
  }

  String _clock(BuildContext context, DateTime t) =>
      MaterialLocalizations.of(context).formatTimeOfDay(TimeOfDay.fromDateTime(t));

  /// "Yesterday · 6:00 PM – 7:00 PM".
  String slot(BuildContext context, SessionTimeDraft draft) =>
      '${day(draft.day, draft)} · ${_clock(context, draft.startAt)} – ${_clock(context, draft.endAt)}';

  /// "Yesterday · 6:00 PM" for a recorded session.
  String recordedAt(BuildContext context, DateTime at, SessionTimeDraft draft) =>
      '${day(DateUtils.dateOnly(at), draft)} · ${_clock(context, at)}';
}
