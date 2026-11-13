import '../models/body_metric_model.dart';
import 'progress_journey_model.dart';

/// A number with its unit: "kg", "lb", "cm", "in" or "%".
class MetricValue {
  final double amount;
  final String unit;

  const MetricValue(this.amount, this.unit);

  static const kg = 'kg';
}

/// Short display number: 95 / 95.5.
extension CompactNumber on double {
  String get compact => this == roundToDouble() ? toStringAsFixed(0) : toStringAsFixed(1);
}

/// Whether a change is progress for the member's goal.
enum ChangeTone { good, bad, neutral }

/// One number tracked over time.
enum ProgressMetric {
  weight,
  bodyFat,
  waist,
  chest,
  hips,
  leftArm,
  rightArm,
  leftThigh,
  rightThigh,
  leftCalf,
  rightCalf,
  neck;

  double? valueOf(BodyMetricEntryModel e) {
    final m = e.measurements;
    return switch (this) {
      weight => e.weight,
      bodyFat => e.bodyFatPercentage,
      waist => m?.waist,
      chest => m?.chest,
      hips => m?.hips,
      leftArm => m?.leftArm,
      rightArm => m?.rightArm,
      leftThigh => m?.leftThigh,
      rightThigh => m?.rightThigh,
      leftCalf => m?.leftCalf,
      rightCalf => m?.rightCalf,
      neck => m?.neck,
    };
  }

  /// Unit shown next to the value.
  String unitFor(BodyMetricEntryModel? e) => switch (this) {
        weight => (e?.weightUnit ?? 'kg').toLowerCase() == 'lbs' ? 'lb' : 'kg',
        bodyFat => '%',
        _ => (e?.measurements?.measurementUnit ?? 'cm').toLowerCase().startsWith('in') ? 'in' : 'cm',
      };

  /// +1: going up is progress, -1: going down is progress, 0: no judgement.
  int progressDirection(ProgressGoalType? goal) => _progressDirections[goal]?[this] ?? 0;

  /// Changes smaller than this are shown as "no change".
  static const double noChange = 0.05;

  ChangeTone toneFor(double delta, ProgressGoalType? goal) {
    final direction = progressDirection(goal);
    if (delta.abs() < noChange || direction == 0) return ChangeTone.neutral;
    return delta.sign == direction ? ChangeTone.good : ChangeTone.bad;
  }
}

/// Which way each metric should move for a goal; metrics not listed are neutral.
const Map<ProgressGoalType, Map<ProgressMetric, int>> _progressDirections = {
  ProgressGoalType.loseFat: {
    ProgressMetric.weight: -1,
    ProgressMetric.bodyFat: -1,
    ProgressMetric.waist: -1,
    ProgressMetric.hips: -1,
  },
  ProgressGoalType.buildMuscle: {
    ProgressMetric.bodyFat: -1,
    ProgressMetric.weight: 1,
    ProgressMetric.chest: 1,
    ProgressMetric.leftArm: 1,
    ProgressMetric.rightArm: 1,
    ProgressMetric.leftThigh: 1,
    ProgressMetric.rightThigh: 1,
    ProgressMetric.leftCalf: 1,
    ProgressMetric.rightCalf: 1,
  },
};

/// Metrics shown together (left/right sides share one chip and chart).
enum MetricGroup {
  weight([ProgressMetric.weight]),
  bodyFat([ProgressMetric.bodyFat]),
  waist([ProgressMetric.waist]),
  chest([ProgressMetric.chest]),
  hips([ProgressMetric.hips]),
  arms([ProgressMetric.leftArm, ProgressMetric.rightArm]),
  thighs([ProgressMetric.leftThigh, ProgressMetric.rightThigh]),
  calves([ProgressMetric.leftCalf, ProgressMetric.rightCalf]),
  neck([ProgressMetric.neck]);

  const MetricGroup(this.metrics);

  final List<ProgressMetric> metrics;
}
