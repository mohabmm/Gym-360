import 'package:gymapp_core/gymapp_core.dart';

/// A body-metric entry on [date] with [values]; coach-measured unless [self].
BodyMetricEntryModel entry(String date, Map<ProgressMetric, double> values, {bool self = false}) {
  double? v(ProgressMetric m) => values[m];
  final hasTape = values.keys.any((m) => m != ProgressMetric.weight && m != ProgressMetric.bodyFat);
  return BodyMetricEntryModel(
    id: '$date-$self',
    userId: 'member-1',
    gymId: 'gym-1',
    date: date,
    weight: v(ProgressMetric.weight),
    weightUnit: 'Kg',
    bodyFatPercentage: v(ProgressMetric.bodyFat),
    measurements: hasTape
        ? BodyMeasurementsModel(
            waist: v(ProgressMetric.waist),
            leftArm: v(ProgressMetric.leftArm),
            measurementUnit: 'Cm',
          )
        : null,
    source: self ? 'SelfReported' : 'TrainerMeasured',
    loggedBy: self ? 'member-1' : 'coach-1',
    createdAt: DateTime.parse(date),
  );
}

/// Weight-only shorthand.
BodyMetricEntryModel weighIn(String date, double kg, {bool self = false}) =>
    entry(date, {ProgressMetric.weight: kg}, self: self);

ProgressJourney journeyOf(List<BodyMetricEntryModel> entries, {ProgressGoalType? goal, double? target}) =>
    ProgressJourney.from(ProgressJourneyModel(
      userId: 'member-1',
      entries: entries,
      goal: goal == null ? null : ProgressGoalModel(goal: goal, targetWeight: target),
    ));
