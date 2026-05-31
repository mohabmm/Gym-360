import 'package:freezed_annotation/freezed_annotation.dart';

part 'staff_model.freezed.dart';
part 'staff_model.g.dart';

@freezed
class StaffModel with _$StaffModel {
  const factory StaffModel({
    required String id,
    required String userId,
    required String gymId,
    required String name,
    String? email,
    String? phone,
    String? photoUrl,
    required String staffRole,
    Map<String, bool>? permissions,
    @Default(true) bool isActive,
    DateTime? createdAt,
  }) = _StaffModel;

  factory StaffModel.fromJson(Map<String, dynamic> json) =>
      _$StaffModelFromJson(json);
}

@freezed
class CreateStaffRequest with _$CreateStaffRequest {
  const factory CreateStaffRequest({
    required String email,
    required String firstName,
    required String lastName,
    String? phone,
    required String staffRole,
    Map<String, bool>? permissions,
  }) = _CreateStaffRequest;

  factory CreateStaffRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateStaffRequestFromJson(json);
}

@freezed
class StaffPermissionModel with _$StaffPermissionModel {
  const factory StaffPermissionModel({
    required String permission,
    required String label,
    String? labelAr,
    required bool owner,
    required bool manager,
    required bool receptionist,
  }) = _StaffPermissionModel;

  factory StaffPermissionModel.fromJson(Map<String, dynamic> json) =>
      _$StaffPermissionModelFromJson(json);
}
