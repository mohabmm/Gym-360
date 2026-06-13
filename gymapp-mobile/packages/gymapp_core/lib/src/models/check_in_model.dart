import 'package:freezed_annotation/freezed_annotation.dart';

part 'check_in_model.freezed.dart';
part 'check_in_model.g.dart';

@freezed
class CheckInModel with _$CheckInModel {
  const factory CheckInModel({
    required String id,
    required String gymId,
    required String memberId,
    required String memberName,
    String? memberPhotoUrl,
    String? memberNumber,
    required String method,
    required DateTime checkedInAt,
    DateTime? checkedOutAt,
    String? status,
  }) = _CheckInModel;

  factory CheckInModel.fromJson(Map<String, dynamic> json) =>
      _$CheckInModelFromJson(json);
}

@freezed
class CheckInQrRequest with _$CheckInQrRequest {
  const factory CheckInQrRequest({
    required String qrData,
  }) = _CheckInQrRequest;

  factory CheckInQrRequest.fromJson(Map<String, dynamic> json) =>
      _$CheckInQrRequestFromJson(json);
}

@freezed
class CheckInNumberRequest with _$CheckInNumberRequest {
  const factory CheckInNumberRequest({
    required String memberNumber,
  }) = _CheckInNumberRequest;

  factory CheckInNumberRequest.fromJson(Map<String, dynamic> json) =>
      _$CheckInNumberRequestFromJson(json);
}

@freezed
class CheckInResultModel with _$CheckInResultModel {
  const factory CheckInResultModel({
    required bool success,
    String? memberName,
    String? memberPhotoUrl,
    String? gymName,
    String? errorCode,
    String? errorMessage,
  }) = _CheckInResultModel;

  factory CheckInResultModel.fromJson(Map<String, dynamic> json) =>
      _$CheckInResultModelFromJson(json);
}
