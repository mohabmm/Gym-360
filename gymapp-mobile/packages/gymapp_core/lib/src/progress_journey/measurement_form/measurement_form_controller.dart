import 'package:flutter/widgets.dart';

import '../progress_journey.dart';
import '../progress_journey_model.dart';
import '../progress_metric.dart';
import 'measurement_rules.dart';

/// Change of one metric against its last reading (for the live chip and the saved summary).
class LiveChange {
  final ProgressMetric metric;
  final MetricValue delta;
  final ChangeTone tone;

  const LiveChange(this.metric, this.delta, this.tone);
}

/// Text fields, focus order and validation for one check-in (weight, body fat, tape, notes).
class MeasurementFormController extends ChangeNotifier {
  final Map<ProgressMetric, TextEditingController> fields = {
    for (final m in checkInMetrics) m: TextEditingController(),
  };
  final Map<ProgressMetric, FocusNode> focus = {for (final m in checkInMetrics) m: FocusNode()};
  final TextEditingController notes = TextEditingController();

  Map<ProgressMetric, LastReading> _last = const {};
  ProgressGoalType? _goal;

  MeasurementFormController() {
    for (final c in [...fields.values, notes]) {
      c.addListener(notifyListeners);
    }
  }

  /// Feeds the client's history (last values + goal) once the journey loads.
  void useJourney(ProgressJourney? journey) {
    _last = lastReadings(journey);
    _goal = journey?.goalType;
  }

  LastReading? last(ProgressMetric m) => _last[m];

  MetricCheck check(ProgressMetric m) => checkMetric(m, fields[m]!.text, previous: _last[m]?.value);

  /// Live change vs the last reading; null without both values.
  LiveChange? change(ProgressMetric m) {
    final now = check(m);
    final before = _last[m];
    if (!now.isValid || before == null) return null;
    final delta = now.value! - before.value;
    return LiveChange(m, MetricValue(delta, unitOf(m)), m.toneFor(delta, _goal));
  }

  /// The unit the form records [m] in.
  static String unitOf(ProgressMetric m) => switch (m) {
        ProgressMetric.weight => MetricValue.kg,
        ProgressMetric.bodyFat => '%',
        _ => 'cm',
      };

  int get filledCount => checkInMetrics.where((m) => check(m).isValid).length;

  bool get hasErrors => checkInMetrics.any((m) => check(m).outOfRange);

  bool get isDirty => fields.values.any((c) => c.text.trim().isNotEmpty) || notes.text.trim().isNotEmpty;

  bool get canSubmit => filledCount > 0 && !hasErrors;

  /// Changes for every filled metric that has a last reading, in form order.
  List<LiveChange> get changes => checkInMetrics.map(change).whereType<LiveChange>().toList();

  double? _value(ProgressMetric m) => check(m).isValid ? check(m).value : null;

  /// The reading part of the body-metric request: weight, body fat, tape (cm) and notes.
  Map<String, dynamic> reading() {
    final tape = {for (final m in checkInTapeMetrics) apiKeyOf(m): _value(m)};
    final hasTape = tape.values.any((v) => v != null);
    final note = notes.text.trim();
    return {
      'weight': _value(ProgressMetric.weight),
      'weightUnit': 'kg',
      'bodyFatPercentage': _value(ProgressMetric.bodyFat),
      'measurements': hasTape ? {...tape, 'measurementUnit': 'cm'} : null,
      'notes': note.isEmpty ? null : note,
    };
  }

  @override
  void dispose() {
    for (final c in [...fields.values, notes]) {
      c.dispose();
    }
    for (final f in focus.values) {
      f.dispose();
    }
    super.dispose();
  }
}
