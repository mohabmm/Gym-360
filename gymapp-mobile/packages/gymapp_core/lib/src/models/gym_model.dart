import 'package:freezed_annotation/freezed_annotation.dart';
import 'gym_branding_model.dart';

part 'gym_model.freezed.dart';
part 'gym_model.g.dart';

@freezed
class GymModel with _$GymModel {
  const factory GymModel({
    required String id,
    required String name,
    String? nameAr,
    String? description,
    String? descriptionAr,
    required String gymCode,
    String? phone,
    String? email,
    required AddressModel address,
    GymBrandingModel? branding,
    required String status,
    int? activeMemberCount,
    int? activeTrainerCount,
    int? activeStaffCount,
    List<OperatingHoursModel>? operatingHours,
    List<String>? amenities,
    List<GymSectionModel>? sections,
    GymSessionPolicyModel? sessionPolicy,
    DateTime? createdAt,
  }) = _GymModel;

  factory GymModel.fromJson(Map<String, dynamic> json) =>
      _$GymModelFromJson(json);
}

@freezed
class AddressModel with _$AddressModel {
  const factory AddressModel({
    String? street,
    String? city,
    String? area,
    String? governorate,
    String? postalCode,
    double? latitude,
    double? longitude,
  }) = _AddressModel;

  factory AddressModel.fromJson(Map<String, dynamic> json) =>
      _$AddressModelFromJson(json);
}

@freezed
class OperatingHoursModel with _$OperatingHoursModel {
  const factory OperatingHoursModel({
    required int dayOfWeek,
    required String openTime,
    required String closeTime,
    @Default(false) bool isClosed,
    @Default(false) bool is24Hours,
  }) = _OperatingHoursModel;

  factory OperatingHoursModel.fromJson(Map<String, dynamic> json) =>
      _$OperatingHoursModelFromJson(json);
}

@freezed
class GymSeasonalHoursModel with _$GymSeasonalHoursModel {
  const factory GymSeasonalHoursModel({
    required String id,
    required String name,
    String? nameAr,
    required DateTime startDate,
    required DateTime endDate,
    required List<OperatingHoursModel> hours,
    @Default(true) bool isActive,
  }) = _GymSeasonalHoursModel;

  factory GymSeasonalHoursModel.fromJson(Map<String, dynamic> json) =>
      _$GymSeasonalHoursModelFromJson(json);
}

@freezed
class GymSectionModel with _$GymSectionModel {
  const factory GymSectionModel({
    required String id,
    required String type,
    String? name,
    String? nameAr,
    String? description,
  }) = _GymSectionModel;

  factory GymSectionModel.fromJson(Map<String, dynamic> json) =>
      _$GymSectionModelFromJson(json);
}

/// Mirrors BE `SessionPolicyDto`: per-gym session cancellation/no-show policy.
/// Default `cancellationWindowHours` is 24, configurable by gym staff/owner
/// via the admin web app. The trainee app uses `cancellationWindowHours` to
/// gate the in-app cancel button.
@freezed
class GymSessionPolicyModel with _$GymSessionPolicyModel {
  const factory GymSessionPolicyModel({
    required String gymId,
    required int cancellationWindowHours,
    required int noshowThreshold,
    required int noshowWindowDays,
    required String noshowAction,
  }) = _GymSessionPolicyModel;

  factory GymSessionPolicyModel.fromJson(Map<String, dynamic> json) =>
      _$GymSessionPolicyModelFromJson(json);
}
