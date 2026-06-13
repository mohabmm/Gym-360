import 'package:freezed_annotation/freezed_annotation.dart';

part 'user_model.freezed.dart';
part 'user_model.g.dart';

@freezed
class UserModel with _$UserModel {
  const UserModel._();

  const factory UserModel({
    required String id,
    required String email,
    required String firstName,
    required String lastName,
    String? phone,
    // BE profile uses `profilePhotoUrl`; legacy payloads used `photoUrl`.
    // Accept both — the getter `photoUrl` returns whichever is non-null.
    @JsonKey(name: 'profilePhotoUrl') String? profilePhotoUrl,
    @JsonKey(name: 'photoUrl') String? legacyPhotoUrl,
    required String role,
    String? gender,
    DateTime? dateOfBirth,
    double? height,
    double? weight,
    String? fitnessLevel,
    List<String>? healthConditions,
    List<String>? goals,
    String? status,
    String? language,
    String? gymId,
    String? gymName,
    @JsonKey(toJson: _gymMembershipsToJson) List<GymMembershipModel>? gymMemberships,
    DateTime? createdAt,
    DateTime? lastLoginAt,
    String? memberNumber,
    String? nationalId,
  }) = _UserModel;

  factory UserModel.fromJson(Map<String, dynamic> json) =>
      _$UserModelFromJson(json);

  String? get photoUrl => profilePhotoUrl ?? legacyPhotoUrl;
}

@freezed
class GymMembershipModel with _$GymMembershipModel {
  const factory GymMembershipModel({
    // BE /profile returns gymId, role, joinedAt, isActive, section, memberNumber.
    // Extra gym-scoped lookups (gymName, logo, plan, status, assigned trainer)
    // come from the gyms module and are merged in by the UI layer, so they're
    // all optional here.
    required String gymId,
    String? role,
    DateTime? joinedAt,
    bool? isActive,
    String? section,
    String? memberNumber,
    String? id,
    String? gymName,
    String? gymLogoUrl,
    String? membershipType,
    String? status,
    DateTime? startDate,
    DateTime? endDate,
    String? assignedTrainerId,
    String? assignedTrainerName,
  }) = _GymMembershipModel;

  factory GymMembershipModel.fromJson(Map<String, dynamic> json) =>
      _$GymMembershipModelFromJson(json);
}

@freezed
class UserConsentModel with _$UserConsentModel {
  const factory UserConsentModel({
    required String id,
    required String userId,
    required String consentType,
    required bool isGranted,
    required DateTime consentedAt,
    String? consentText,
    String? ipAddress,
  }) = _UserConsentModel;

  factory UserConsentModel.fromJson(Map<String, dynamic> json) =>
      _$UserConsentModelFromJson(json);
}

@freezed
class UserProfileUpdateRequest with _$UserProfileUpdateRequest {
  const factory UserProfileUpdateRequest({
    String? firstName,
    String? lastName,
    String? phone,
    String? gender,
    DateTime? dateOfBirth,
    double? height,
    double? weight,
    String? fitnessLevel,
    List<String>? healthConditions,
    List<String>? goals,
    String? photoUrl,
  }) = _UserProfileUpdateRequest;

  factory UserProfileUpdateRequest.fromJson(Map<String, dynamic> json) =>
      _$UserProfileUpdateRequestFromJson(json);
}

List<Map<String, dynamic>>? _gymMembershipsToJson(
        List<GymMembershipModel>? memberships) =>
    memberships?.map((e) => e.toJson()).toList();
