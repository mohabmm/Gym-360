import 'package:freezed_annotation/freezed_annotation.dart';

part 'plan_features_model.freezed.dart';
part 'plan_features_model.g.dart';

/// Response from GET /api/v1/billing/plan-features.
///
/// Mirrors `PlanFeaturesResponse` on the backend: the gym's active plan,
/// its flat limits, and the list of feature toggles (with any per-gym
/// overrides applied).
@freezed
class PlanFeaturesModel with _$PlanFeaturesModel {
  const PlanFeaturesModel._();

  const factory PlanFeaturesModel({
    required String gymId,
    required String planCode,
    required String planName,
    required int memberLimit,
    required int trainerLimit,
    required int staffLimit,
    required List<PlanFeatureItemModel> features,
  }) = _PlanFeaturesModel;

  factory PlanFeaturesModel.fromJson(Map<String, dynamic> json) =>
      _$PlanFeaturesModelFromJson(json);

  /// Check if a specific feature is enabled (fails closed if unknown).
  bool isEnabled(String featureCode) {
    for (final f in features) {
      if (f.featureCode == featureCode) return f.isEnabled;
    }
    return false;
  }

  /// Look up the numeric limit value for a feature, if any.
  int? limitFor(String featureCode) {
    for (final f in features) {
      if (f.featureCode == featureCode) return f.limitValue;
    }
    return null;
  }
}

@freezed
class PlanFeatureItemModel with _$PlanFeatureItemModel {
  const factory PlanFeatureItemModel({
    required String featureCode,
    required String featureName,
    required bool isEnabled,
    int? limitValue,
    @Default(false) bool isOverridden,
  }) = _PlanFeatureItemModel;

  factory PlanFeatureItemModel.fromJson(Map<String, dynamic> json) =>
      _$PlanFeatureItemModelFromJson(json);
}

@freezed
class PlanLimitsModel with _$PlanLimitsModel {
  const factory PlanLimitsModel({
    required int maxMembers,
    required int maxTrainers,
    required int maxStaff,
    int? maxClasses,
    int? maxAnnouncements,
  }) = _PlanLimitsModel;

  factory PlanLimitsModel.fromJson(Map<String, dynamic> json) =>
      _$PlanLimitsModelFromJson(json);
}

@freezed
class PlanStorageLimitsModel with _$PlanStorageLimitsModel {
  const factory PlanStorageLimitsModel({
    required int maxProgressPhotosPerMember,
    required int maxExerciseMediaPerGym,
    required int maxStorageMb,
    int? currentStorageUsedMb,
  }) = _PlanStorageLimitsModel;

  factory PlanStorageLimitsModel.fromJson(Map<String, dynamic> json) =>
      _$PlanStorageLimitsModelFromJson(json);
}

@freezed
class MembershipPlanModel with _$MembershipPlanModel {
  const factory MembershipPlanModel({
    required String id,
    required String name,
    String? nameAr,
    String? description,
    required double monthlyPrice,
    double? annualPrice,
    required String currency,
    required Map<String, bool> features,
    required PlanLimitsModel limits,
    @Default(true) bool isPublic,
    DateTime? availableFrom,
    DateTime? availableUntil,
    int? gymCount,
    double? mrr,
  }) = _MembershipPlanModel;

  factory MembershipPlanModel.fromJson(Map<String, dynamic> json) =>
      _$MembershipPlanModelFromJson(json);
}
