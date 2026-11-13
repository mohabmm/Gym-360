import '../models/body_metric_model.dart';
import 'progress_journey_model.dart';
import 'progress_metric.dart';

/// Change of one metric between day one and now.
class MetricChange {
  final ProgressMetric metric;
  final double first;
  final double latest;
  final String unit;
  final ChangeTone tone;

  const MetricChange({
    required this.metric,
    required this.first,
    required this.latest,
    required this.unit,
    required this.tone,
  });

  double get delta => latest - first;

  MetricValue get firstValue => MetricValue(first, unit);
  MetricValue get latestValue => MetricValue(latest, unit);
  MetricValue get deltaValue => MetricValue(delta, unit);
}

/// One point on a metric's chart.
class MetricPoint {
  final DateTime date;
  final double value;
  final bool coachMeasured;

  const MetricPoint({required this.date, required this.value, required this.coachMeasured});
}

/// One check-in in the timeline, with what changed since the previous one.
class CheckIn {
  final BodyMetricEntryModel entry;
  final DateTime date;
  final List<MetricChange> changes;
  final bool isDayOne;

  const CheckIn({required this.entry, required this.date, required this.changes, required this.isDayOne});
}

/// Who measured an entry.
enum MeasuredBy { coach, member, inBody }

/// pt.progress_measurements: the member's progress from day one, derived from all entries.
///
/// Day one is the coach's first measurement; with no coach entry yet it's the earliest entry
/// ([baselineIsSelfReported]). Each metric's change uses the latest entry that has it.
class ProgressJourney {
  /// All entries, oldest first.
  final List<BodyMetricEntryModel> entries;
  final BodyMetricEntryModel? baseline;
  final bool baselineIsSelfReported;
  final ProgressGoalModel? goal;

  const ProgressJourney._(this.entries, this.baseline, this.baselineIsSelfReported, this.goal);

  factory ProgressJourney.from(ProgressJourneyModel model) {
    final sorted = [...model.entries]..sort(_byDate);
    final coachFirst = sorted.where((e) => measuredBy(e) == MeasuredBy.coach).firstOrNull;
    final baseline = coachFirst ?? sorted.firstOrNull;
    return ProgressJourney._(sorted, baseline, coachFirst == null && baseline != null, model.goal);
  }

  static int _byDate(BodyMetricEntryModel a, BodyMetricEntryModel b) {
    final byDay = dateOf(a).compareTo(dateOf(b));
    return byDay != 0 ? byDay : a.createdAt.compareTo(b.createdAt);
  }

  static DateTime dateOf(BodyMetricEntryModel e) => DateTime.tryParse(e.date) ?? e.createdAt;

  static MeasuredBy measuredBy(BodyMetricEntryModel e) => switch (e.source.toLowerCase()) {
        'trainermeasured' => MeasuredBy.coach,
        'inbodyscan' => MeasuredBy.inBody,
        _ => MeasuredBy.member,
      };

  ProgressGoalType? get goalType => goal?.goal;
  bool get isEmpty => baseline == null;

  /// Entries from day one on (earlier self-logged entries are kept in the timeline only).
  List<BodyMetricEntryModel> get fromDayOne {
    final start = baseline;
    if (start == null) return const [];
    return entries.skipWhile((e) => !identical(e, start)).toList();
  }

  /// True while day one is the only check-in.
  bool get hasOnlyBaseline => fromDayOne.length == 1;

  BodyMetricEntryModel? get latest => entries.lastOrNull;

  DateTime? get startDate => baseline == null ? null : dateOf(baseline!);

  int daysSinceStart(DateTime now) {
    final start = startDate;
    if (start == null) return 0;
    final today = DateTime(now.year, now.month, now.day);
    return today.difference(DateTime(start.year, start.month, start.day)).inDays.clamp(0, 1 << 30);
  }

  /// Day one → latest for [metric]; null without at least two readings from day one on.
  MetricChange? change(ProgressMetric metric) {
    final readings = fromDayOne.where((e) => metric.valueOf(e) != null).toList();
    if (readings.length < 2) return null;
    return _changeBetween(metric, readings.first, readings.last);
  }

  MetricChange _changeBetween(ProgressMetric metric, BodyMetricEntryModel from, BodyMetricEntryModel to) {
    final first = metric.valueOf(from)!;
    final last = metric.valueOf(to)!;
    return MetricChange(
      metric: metric,
      first: first,
      latest: last,
      unit: metric.unitFor(to),
      tone: metric.toneFor(last - first, goalType),
    );
  }

  /// Every metric with at least one reading from day one on, in display order.
  List<ProgressMetric> get trackedMetrics =>
      ProgressMetric.values.where((m) => fromDayOne.any((e) => m.valueOf(e) != null)).toList();

  /// Chip groups that have data.
  List<MetricGroup> get trackedGroups {
    final tracked = trackedMetrics.toSet();
    return MetricGroup.values.where((g) => g.metrics.any(tracked.contains)).toList();
  }

  /// Chart points for [metric] from day one on.
  List<MetricPoint> series(ProgressMetric metric) => [
        for (final e in fromDayOne)
          if (metric.valueOf(e) case final v?)
            MetricPoint(date: dateOf(e), value: v, coachMeasured: measuredBy(e) == MeasuredBy.coach),
      ];

  /// Share (0–1) of the way from the day-one weight to the target weight; null without a target.
  double? get targetProgress {
    final target = goal?.targetWeight;
    final start = _startWeight;
    if (target == null || start == null) return null;
    final distance = start - target;
    if (distance.abs() < ProgressMetric.noChange) return null;
    final now = change(ProgressMetric.weight)?.latest ?? start;
    return ((start - now) / distance).clamp(0.0, 1.0);
  }

  /// Day-one weight (from the weight change, or the baseline entry while it is the only reading).
  double? get _startWeight {
    final fromChange = change(ProgressMetric.weight)?.first;
    if (fromChange != null) return fromChange;
    final start = baseline;
    return start == null ? null : ProgressMetric.weight.valueOf(start);
  }

  /// Check-ins newest first, each with what changed since the previous check-in.
  List<CheckIn> get timeline {
    final items = <CheckIn>[];
    for (var i = 0; i < entries.length; i++) {
      final entry = entries[i];
      final previous = i == 0 ? null : entries[i - 1];
      items.add(CheckIn(
        entry: entry,
        date: dateOf(entry),
        changes: previous == null ? const [] : _changesSince(previous, entry),
        isDayOne: identical(entry, baseline),
      ));
    }
    return items.reversed.toList();
  }

  List<MetricChange> _changesSince(BodyMetricEntryModel previous, BodyMetricEntryModel entry) => [
        for (final m in ProgressMetric.values)
          if (m.valueOf(previous) != null && m.valueOf(entry) != null) _changeBetween(m, previous, entry),
      ];
}
