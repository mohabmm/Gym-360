import 'package:freezed_annotation/freezed_annotation.dart';

part 'body_metric_model.freezed.dart';
part 'body_metric_model.g.dart';

/// Matches the backend BodyMetricDto shape per docs/01_GYMAPP_BACKEND_COMPLETE.md MODULE 4.
/// `date` is the local calendar day the metric represents (YYYY-MM-DD string).
@freezed
class BodyMetricEntryModel with _$BodyMetricEntryModel {
  const factory BodyMetricEntryModel({
    required String id,
    required String userId,
    required String gymId,
    required String date,
    double? weight,
    String? weightUnit,
    double? bodyFatPercentage,
    BodyMeasurementsModel? measurements,
    required String source,
    required String loggedBy,
    String? notes,
    required DateTime createdAt,
  }) = _BodyMetricEntryModel;

  factory BodyMetricEntryModel.fromJson(Map<String, dynamic> json) =>
      _$BodyMetricEntryModelFromJson(json);
}

/// Matches the backend BodyMeasurementsDto — ten body-part measurements + a shared unit label.
@freezed
class BodyMeasurementsModel with _$BodyMeasurementsModel {
  const factory BodyMeasurementsModel({
    double? chest,
    double? waist,
    double? hips,
    double? leftArm,
    double? rightArm,
    double? leftThigh,
    double? rightThigh,
    double? leftCalf,
    double? rightCalf,
    double? neck,
    String? measurementUnit,
  }) = _BodyMeasurementsModel;

  factory BodyMeasurementsModel.fromJson(Map<String, dynamic> json) =>
      _$BodyMeasurementsModelFromJson(json);
}

/// Matches backend ProgressDashboardResponse (Modules/Progress). BE returns a
/// flat summary — `latestMetrics` carries the most-recent body metric (with
/// weight), change deltas are from the first entry, and streak counts cover
/// both workouts and check-ins. Fields are nullable because the BE omits some
/// when the user has no data yet; the UI handles `null` as "no data".
@freezed
class ProgressDashboardModel with _$ProgressDashboardModel {
  const factory ProgressDashboardModel({
    BodyMetricEntryModel? latestMetrics,
    double? weightChangeFromFirst,
    double? bodyFatChangeFromFirst,
    @Default(0) int photoCount,
    @Default(0) int currentWorkoutStreak,
    @Default(0) int longestWorkoutStreak,
    @Default(0) int currentCheckInStreak,
    @Default(0) int longestCheckInStreak,
  }) = _ProgressDashboardModel;

  factory ProgressDashboardModel.fromJson(Map<String, dynamic> json) =>
      _$ProgressDashboardModelFromJson(json);
}

@freezed
class WeightTrendPoint with _$WeightTrendPoint {
  const factory WeightTrendPoint({
    required DateTime date,
    required double weight,
  }) = _WeightTrendPoint;

  factory WeightTrendPoint.fromJson(Map<String, dynamic> json) =>
      _$WeightTrendPointFromJson(json);
}
