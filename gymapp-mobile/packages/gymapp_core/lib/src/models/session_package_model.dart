// ignore_for_file: invalid_annotation_target

import 'package:freezed_annotation/freezed_annotation.dart';

part 'session_package_model.freezed.dart';
part 'session_package_model.g.dart';

@freezed
class SessionPackageModel with _$SessionPackageModel {
  const factory SessionPackageModel({
    required String id,
    @Default('') String gymId,
    @Default('') String memberId,
    String? memberName,
    @Default('') String trainerId,
    String? trainerName,
    @Default('') String templateId,
    String? packageName,
    @Default(0) int totalSessions,
    @Default(0) int remainingSessions,
    @Default(0) int usedSessions,
    @Default(0) double price,
    @Default('EGP') String currency,
    @Default('Active') String status,
    DateTime? startDate,
    DateTime? expiryDate,
    DateTime? createdAt,
    // PRD /me/packages fields.
    String? name,
    @JsonKey(fromJson: _moneyFromJson, toJson: _moneyToJson)
    MoneyModel? pricePerSession,
    @JsonKey(fromJson: _moneyFromJson, toJson: _moneyToJson)
    MoneyModel? totalPrice,
    DateTime? assignedAt,
    DateTime? expiresAt,
    DateTime? lastUsedAt,
  }) = _SessionPackageModel;

  factory SessionPackageModel.fromJson(Map<String, dynamic> json) =>
      _$SessionPackageModelFromJson(json);
}

// Billing endpoints disagree on the money shape: /me/packages returns
// `{ "amount": 150, "currency": "EGP" }` (MoneyDto), while the flat
// SessionPackageDto on /my-clients returns a raw number (150.0). Accept both
// so the same model works for trainer and trainee surfaces.
MoneyModel? _moneyFromJson(Object? value) {
  if (value == null) return null;
  if (value is num) {
    return MoneyModel(amount: value.toDouble());
  }
  if (value is Map<String, dynamic>) {
    return MoneyModel.fromJson(value);
  }
  return null;
}

Map<String, dynamic>? _moneyToJson(MoneyModel? value) => value?.toJson();

/// `{ "amount": <decimal>, "currency": "EGP" }` money envelope used across
/// billing responses (PRD /me endpoints). Currency is EGP only in MVP per
/// Founder Q10.
@freezed
class MoneyModel with _$MoneyModel {
  const factory MoneyModel({
    required double amount,
    @Default('EGP') String currency,
  }) = _MoneyModel;

  factory MoneyModel.fromJson(Map<String, dynamic> json) =>
      _$MoneyModelFromJson(json);
}

@freezed
class SessionPackageTemplateModel with _$SessionPackageTemplateModel {
  const factory SessionPackageTemplateModel({
    required String id,
    required String gymId,
    required String name,
    String? nameAr,
    required int sessionCount,
    required double totalPrice,
    required String currency,
    int? expiryDays,
    @Default(true) bool isActive,
    DateTime? createdAt,
  }) = _SessionPackageTemplateModel;

  factory SessionPackageTemplateModel.fromJson(Map<String, dynamic> json) =>
      _$SessionPackageTemplateModelFromJson(json);
}

@freezed
class AssignPackageRequest with _$AssignPackageRequest {
  const factory AssignPackageRequest({
    required String memberId,
    required String trainerId,
    required String templateId,
  }) = _AssignPackageRequest;

  factory AssignPackageRequest.fromJson(Map<String, dynamic> json) =>
      _$AssignPackageRequestFromJson(json);
}
