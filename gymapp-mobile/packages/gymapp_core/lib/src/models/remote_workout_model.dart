import 'package:freezed_annotation/freezed_annotation.dart';

part 'remote_workout_model.freezed.dart';
part 'remote_workout_model.g.dart';

@freezed
class RemoteWorkoutStatusModel with _$RemoteWorkoutStatusModel {
  const factory RemoteWorkoutStatusModel({
    String? clientId,
    String? trainerId,
    @Default(false) bool isActive,
    String? reason,
    DateTime? activatedAt,
    DateTime? deactivatedAt,
  }) = _RemoteWorkoutStatusModel;

  factory RemoteWorkoutStatusModel.fromJson(Map<String, dynamic> json) =>
      _$RemoteWorkoutStatusModelFromJson(json);
}

@freezed
class ActivateRemoteWorkoutRequest with _$ActivateRemoteWorkoutRequest {
  const factory ActivateRemoteWorkoutRequest({
    required String clientId,
    String? reason,
  }) = _ActivateRemoteWorkoutRequest;

  factory ActivateRemoteWorkoutRequest.fromJson(Map<String, dynamic> json) =>
      _$ActivateRemoteWorkoutRequestFromJson(json);
}

@freezed
class DeactivateRemoteWorkoutRequest with _$DeactivateRemoteWorkoutRequest {
  const factory DeactivateRemoteWorkoutRequest({
    required String clientId,
  }) = _DeactivateRemoteWorkoutRequest;

  factory DeactivateRemoteWorkoutRequest.fromJson(Map<String, dynamic> json) =>
      _$DeactivateRemoteWorkoutRequestFromJson(json);
}
