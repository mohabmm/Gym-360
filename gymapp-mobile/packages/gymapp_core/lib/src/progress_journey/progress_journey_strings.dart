import 'package:intl/intl.dart';

import 'progress_journey_model.dart';
import 'progress_metric.dart';

/// Arabic-first labels for the progress journey (pt.progress_measurements).
class ProgressJourneyStrings {
  final bool isArabic;

  const ProgressJourneyStrings(this.isArabic);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get sinceDayOne => _t('منذ اليوم الأول', 'Since day one');
  String dayOneOn(DateTime d) => _t('اليوم الأول · ${date(d)}', 'Day 1 · ${date(d)}');
  String daysAgo(Duration elapsed) => switch (elapsed.inDays) {
        0 => _t('اليوم', 'Today'),
        1 => _t('منذ يوم', '1 day ago'),
        2 => _t('منذ يومين', '2 days ago'),
        final days when days <= 10 => _t('منذ $days أيام', '$days days ago'),
        final days => _t('منذ $days يومًا', '$days days ago'),
      };
  String get start => _t('البداية', 'Start');
  String get now => _t('الآن', 'Now');
  String get change => _t('التغيير', 'Change');
  String get dayOne => _t('اليوم الأول', 'Day 1');
  String get dayOneVsNow => _t('اليوم الأول مقابل الآن', 'Day 1 vs now');
  String get trend => _t('التطور', 'Trend');
  String get checkIns => _t('سجل القياسات', 'Check-ins');
  String get selfReportedBaseline =>
      _t('البداية من قياس سجّلته بنفسك', 'Starting point is self-reported');
  String get baselineSet => _t('تم تسجيل نقطة البداية', 'Your baseline is set');
  String get baselineSetHint =>
      _t('القياس القادم سيُظهر تقدمك', 'Your next check-in shows your progress');
  String get noChange => _t('بدون تغيير', 'No change');
  String get noteLabel => _t('ملاحظة المدرب', 'Coach note');
  String get setGoal => _t('حدد هدفك', 'Set a goal');
  String get editGoal => _t('تعديل الهدف', 'Edit goal');
  String get goalTitle => _t('الهدف', 'Goal');
  String get targetWeight => _t('الوزن المستهدف (كجم)', 'Target weight (kg)');
  String get targetOptional => _t('اختياري', 'Optional');
  String get save => _t('حفظ', 'Save');
  String get invalidTarget => _t('أدخل وزنًا بين 1 و 500', 'Enter a weight between 1 and 500');
  /// [progress] is 0–1 of the way from day one to [target].
  String towardTarget(double progress, MetricValue target) {
    final percent = (progress * 100).round();
    final to = value(target);
    return _t('$percent٪ من الطريق إلى $to', '$percent% of the way to $to');
  }
  String get coach => _t('المدرب', 'Coach');
  String coachNamed(String name) => _t('الكابتن $name', 'Coach $name');
  String get you => _t('أنت', 'You');
  String get client => _t('العميل', 'Client');
  String get inBody => 'InBody';
  String get range1m => _t('شهر', '1M');
  String get range3m => _t('3 شهور', '3M');
  String get rangeAll => _t('الكل', 'All');
  String get memberEmptyTitle => _t('لم تبدأ القياسات بعد', 'No measurements yet');
  String get memberEmptyBody => _t(
        'سيأخذ مدربك قياساتك الأولى في أول حصة، وستتابع تقدمك من هنا.',
        'Your coach will take your first measurements in your first session. Your progress shows here.',
      );
  String get coachEmptyTitle => _t('لا توجد قياسات بعد', 'No measurements yet');
  String get coachEmptyBody => _t(
        'سجّل القياسات في أول حصة لتحديد نقطة البداية.',
        'Record measurements in the first session to set the baseline.',
      );
  String get recordMeasurements => _t('تسجيل القياسات', 'Record measurements');

  String goal(ProgressGoalType? g) => switch (g) {
        ProgressGoalType.loseFat => _t('خسارة الدهون', 'Lose fat'),
        ProgressGoalType.buildMuscle => _t('بناء العضلات', 'Build muscle'),
        ProgressGoalType.generalFitness => _t('لياقة عامة', 'General fitness'),
        null => _t('بدون هدف', 'No goal set'),
      };

  String metric(ProgressMetric m) => switch (m) {
        ProgressMetric.weight => _t('الوزن', 'Weight'),
        ProgressMetric.bodyFat => _t('نسبة الدهون', 'Body fat'),
        ProgressMetric.waist => _t('الوسط', 'Waist'),
        ProgressMetric.chest => _t('الصدر', 'Chest'),
        ProgressMetric.hips => _t('الأرداف', 'Hips'),
        ProgressMetric.leftArm => _t('الذراع الأيسر', 'Left arm'),
        ProgressMetric.rightArm => _t('الذراع الأيمن', 'Right arm'),
        ProgressMetric.leftThigh => _t('الفخذ الأيسر', 'Left thigh'),
        ProgressMetric.rightThigh => _t('الفخذ الأيمن', 'Right thigh'),
        ProgressMetric.leftCalf => _t('السمانة اليسرى', 'Left calf'),
        ProgressMetric.rightCalf => _t('السمانة اليمنى', 'Right calf'),
        ProgressMetric.neck => _t('الرقبة', 'Neck'),
      };

  String group(MetricGroup g) => switch (g) {
        MetricGroup.arms => _t('الذراعان', 'Arms'),
        MetricGroup.thighs => _t('الفخذان', 'Thighs'),
        MetricGroup.calves => _t('السمانة', 'Calves'),
        _ => metric(g.metrics.first),
      };

  String unitOf(MetricValue v) => switch (v.unit) {
        'kg' => _t('كجم', 'kg'),
        'lb' => _t('رطل', 'lb'),
        'cm' => _t('سم', 'cm'),
        'in' => _t('بوصة', 'in'),
        final other => other,
      };

  /// "95.5 kg" / "24%".
  String value(MetricValue v) => v.unit == '%' ? '${v.amount.compact}%' : '${v.amount.compact} ${unitOf(v)}';

  /// "↓ 4.2 kg" / "↑ 1 cm" / "No change".
  String delta(MetricValue change) {
    if (change.amount.abs() < ProgressMetric.noChange) return noChange;
    final arrow = change.amount < 0 ? '↓' : '↑';
    return '$arrow ${value(MetricValue(change.amount.abs(), change.unit))}';
  }

  String date(DateTime d) => DateFormat('d MMM yyyy', isArabic ? 'ar' : 'en').format(d);
  String shortDate(DateTime d) => DateFormat('d MMM', isArabic ? 'ar' : 'en').format(d);
}
