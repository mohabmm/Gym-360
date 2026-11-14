import 'package:intl/intl.dart';

import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'measurement_form_controller.dart';
import 'measurement_rules.dart';

/// Arabic / English copy for the check-in form (coach and member).
class MeasurementFormStrings {
  final bool isArabic;
  final ProgressJourneyStrings journey;

  MeasurementFormStrings(this.isArabic) : journey = ProgressJourneyStrings(isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get body => _t('الجسم', 'Body');
  String get tape => _t('مقاسات الجسم (سم)', 'Tape measurements (cm)');
  String get tapeHint => _t('املأ ما تحتاجه فقط', 'Fill only what you need');
  String get left => _t('يسار', 'Left');
  String get right => _t('يمين', 'Right');
  String get notes => _t('ملاحظات (اختياري)', 'Notes (optional)');
  String get notesHint => _t('مثال: صائم، بعد التمرين', 'e.g. fasted, after training');
  String get firstCheckIn => _t('أول قياس — سيصبح نقطة البداية', 'First check-in — this becomes Day 1');
  String get checkInDate => _t('تاريخ القياس', 'Check-in date');
  String get today => _t('اليوم', 'Today');
  String get alreadyRecorded => _t(
        'يوجد قياس مسجل في هذا اليوم بالفعل. اختر تاريخًا آخر.',
        'A check-in already exists on this day. Pick another date.',
      );
  String get bigChange => _t('تغيير كبير عن آخر مرة — تأكد من الرقم', 'Big change since last time — double-check');
  String get enterOne => _t('أدخل قياسًا واحدًا على الأقل', 'Enter at least one measurement');
  String get fixErrors => _t('صحّح القيم المظللة بالأحمر', 'Fix the values marked in red');
  String get save => _t('حفظ', 'Save');
  String get discardTitle => _t('تجاهل القياسات؟', 'Discard measurements?');
  String get discardBody => _t('لن يتم حفظ ما أدخلته.', "What you entered won't be saved.");
  String get discard => _t('تجاهل', 'Discard');
  String get keepEditing => _t('متابعة التعديل', 'Keep editing');
  String get saved => _t('تم حفظ القياسات', 'Measurements saved');
  String get saveFailed => _t('تعذّر الحفظ، حاول مرة أخرى', 'Could not save, please try again');

  String get done => _t('تمام', 'Done');
  String savedTitle(int wins) => wins > 0
      ? _t('أحسنت! $wins تحسّن منذ آخر قياس', wins == 1 ? 'Nice! 1 win since last time' : 'Nice! $wins wins since last time')
      : _t('تم حفظ القياس', 'Check-in saved');
  String savedSubtitle(bool first) => first
      ? _t('هذه نقطة البداية — كل قياس قادم سيُقارن بها.', 'This is your starting point — every next check-in compares to it.')
      : _t('الثبات هو السر. نراك في القياس القادم!', 'Consistency wins. See you at the next check-in!');

  String saveCount(int n) => n == 0 ? save : _t('حفظ $n', 'Save $n');
  String filled(int n) => _t('$n قياس', n == 1 ? '1 measurement' : '$n measurements');

  String range(ProgressMetric m) {
    final r = metricRanges[m]!;
    return _t('أدخل من ${r.min.compact} إلى ${r.max.compact}', 'Enter ${r.min.compact}–${r.max.compact}');
  }

  String last(LastReading r, MetricValue v) => _t(
        'آخر مرة: ${journey.value(v)} · ${journey.shortDate(r.date)}',
        'Last: ${journey.value(v)} · ${journey.shortDate(r.date)}',
      );

  String lastCheckIn(DateTime d, DateTime now) {
    final days = DateTime(now.year, now.month, now.day).difference(DateTime(d.year, d.month, d.day));
    return _t('آخر قياس: ${journey.shortDate(d)} · ${journey.daysAgo(days)}',
        'Last check-in: ${journey.shortDate(d)} · ${journey.daysAgo(days)}');
  }

  String dateLabel(DateTime d, DateTime now) {
    final isToday = d.year == now.year && d.month == now.month && d.day == now.day;
    return isToday ? today : DateFormat('EEE d MMM', isArabic ? 'ar' : 'en').format(d);
  }

  /// "Measurements saved · Weight ↓ 1.2 kg · Waist ↓ 2 cm" (top three changes).
  String savedSummary(List<LiveChange> changes) {
    final parts = [for (final c in changes.take(3)) '${journey.metric(c.metric)} ${journey.delta(c.delta)}'];
    return [saved, ...parts].join(' · ');
  }
}
