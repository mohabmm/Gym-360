import 'package:flutter/services.dart';

import '../../models/body_metric_model.dart';
import '../progress_journey.dart';
import '../progress_metric.dart';

/// Plausible range for one metric, in kg / % / cm. Stricter than the API's bounds on purpose.
class MetricRange {
  final double min;
  final double max;

  const MetricRange(this.min, this.max);

  bool contains(double v) => v >= min && v <= max;
}

const Map<ProgressMetric, MetricRange> metricRanges = {
  ProgressMetric.weight: MetricRange(20, 350),
  ProgressMetric.bodyFat: MetricRange(2, 75),
  ProgressMetric.chest: MetricRange(50, 200),
  ProgressMetric.waist: MetricRange(40, 200),
  ProgressMetric.hips: MetricRange(50, 200),
  ProgressMetric.neck: MetricRange(20, 70),
  ProgressMetric.leftArm: MetricRange(15, 70),
  ProgressMetric.rightArm: MetricRange(15, 70),
  ProgressMetric.leftThigh: MetricRange(25, 110),
  ProgressMetric.rightThigh: MetricRange(25, 110),
  ProgressMetric.leftCalf: MetricRange(20, 70),
  ProgressMetric.rightCalf: MetricRange(20, 70),
};

/// Weight and body fat, then the tape: singles first, left/right pairs side by side.
const List<ProgressMetric> checkInBodyMetrics = [ProgressMetric.weight, ProgressMetric.bodyFat];
const List<List<ProgressMetric>> checkInTapeRows = [
  [ProgressMetric.chest],
  [ProgressMetric.waist],
  [ProgressMetric.hips],
  [ProgressMetric.neck],
  [ProgressMetric.leftArm, ProgressMetric.rightArm],
  [ProgressMetric.leftThigh, ProgressMetric.rightThigh],
  [ProgressMetric.leftCalf, ProgressMetric.rightCalf],
];
final List<ProgressMetric> checkInTapeMetrics = List.unmodifiable([for (final row in checkInTapeRows) ...row]);

/// Every field in keyboard order.
final List<ProgressMetric> checkInMetrics = List.unmodifiable([...checkInBodyMetrics, ...checkInTapeMetrics]);

/// JSON key of [m] in the body-metric request (`bodyFatPercentage`, `measurements.leftArm`, …).
String apiKeyOf(ProgressMetric m) => m == ProgressMetric.bodyFat ? 'bodyFatPercentage' : m.name;

/// A change bigger than this share of the last value is flagged "double-check".
const double bigChangeShare = 0.10;

const _arabicDigits = '٠١٢٣٤٥٦٧٨٩';

/// "٨٢٫٥" / "82,5" / " 82.5 " → "82.5".
String normalizeNumber(String text) {
  final buffer = StringBuffer();
  for (final ch in text.trim().split('')) {
    final arabic = _arabicDigits.indexOf(ch);
    buffer.write(arabic >= 0 ? '$arabic' : (ch == ',' || ch == '٫' ? '.' : ch));
  }
  return buffer.toString();
}

/// Up to 3 digits and 1 decimal; Arabic-Indic digits and comma decimals are converted as typed.
class MeasurementInputFormatter extends TextInputFormatter {
  static final _pattern = RegExp(r'^\d{0,3}(\.\d?)?$');

  const MeasurementInputFormatter();

  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final text = normalizeNumber(newValue.text);
    if (!_pattern.hasMatch(text)) return oldValue;
    return TextEditingValue(text: text, selection: TextSelection.collapsed(offset: text.length));
  }
}

/// What the form knows about one typed value.
class MetricCheck {
  final double? value;
  final bool outOfRange;
  final bool bigChange;

  const MetricCheck({this.value, this.outOfRange = false, this.bigChange = false});

  static const empty = MetricCheck();

  bool get isValid => value != null && !outOfRange;
}

/// Parses [text] for [metric]; [previous] (the last recorded value) enables the big-change warning.
MetricCheck checkMetric(ProgressMetric metric, String text, {double? previous}) {
  final normalized = normalizeNumber(text);
  if (normalized.isEmpty) return MetricCheck.empty;
  final value = double.tryParse(normalized);
  if (value == null) return const MetricCheck(outOfRange: true);
  if (!metricRanges[metric]!.contains(value)) return MetricCheck(value: value, outOfRange: true);
  final big = previous != null && previous > 0 && (value - previous).abs() / previous > bigChangeShare;
  return MetricCheck(value: value, bigChange: big);
}

/// The most recent value of a metric and the day it was recorded.
class LastReading {
  final double value;
  final DateTime date;

  const LastReading(this.value, this.date);
}

/// Latest reading of each metric across all entries (an entry may have only some metrics).
Map<ProgressMetric, LastReading> lastReadings(ProgressJourney? journey) {
  final result = <ProgressMetric, LastReading>{};
  for (final e in (journey?.entries ?? const <BodyMetricEntryModel>[]).reversed) {
    for (final m in ProgressMetric.values) {
      final v = m.valueOf(e);
      if (v != null) result.putIfAbsent(m, () => LastReading(v, ProgressJourney.dateOf(e)));
    }
  }
  return result;
}

bool _sameDay(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;

/// True when the journey already has an entry on [day] (the API keeps one entry per day).
bool hasEntryOn(ProgressJourney? journey, DateTime day) =>
    (journey?.entries ?? const <BodyMetricEntryModel>[]).any((e) => _sameDay(ProgressJourney.dateOf(e), day));

/// `yyyy-MM-dd` for the API.
String apiDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
