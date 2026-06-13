import 'package:freezed_annotation/freezed_annotation.dart';

part 'trainer_availability_model.freezed.dart';
part 'trainer_availability_model.g.dart';

@freezed
class TrainerAvailabilityModel with _$TrainerAvailabilityModel {
  const factory TrainerAvailabilityModel({
    required String id,
    required String trainerId,
    required List<AvailabilitySlotModel> slots,
    required int bufferMinutes,
    List<BlockedDateModel>? blockedDates,
  }) = _TrainerAvailabilityModel;

  factory TrainerAvailabilityModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerAvailabilityModelFromJson(json);
}

@freezed
class AvailabilitySlotModel with _$AvailabilitySlotModel {
  const factory AvailabilitySlotModel({
    required String id,
    required int dayOfWeek,
    required String startTime,
    required String endTime,
    @Default(true) bool isActive,
  }) = _AvailabilitySlotModel;

  factory AvailabilitySlotModel.fromJson(Map<String, dynamic> json) =>
      _$AvailabilitySlotModelFromJson(json);
}

@freezed
class BlockedDateModel with _$BlockedDateModel {
  const factory BlockedDateModel({
    required String id,
    required DateTime date,
    String? reason,
    @Default(true) bool allDay,
    String? startTime,
    String? endTime,
  }) = _BlockedDateModel;

  factory BlockedDateModel.fromJson(Map<String, dynamic> json) =>
      _$BlockedDateModelFromJson(json);
}

@freezed
class BookableSlotModel with _$BookableSlotModel {
  const factory BookableSlotModel({
    required DateTime startTime,
    required DateTime endTime,
    required String trainerId,
    String? trainerName,
    @Default(true) bool isAvailable,
  }) = _BookableSlotModel;

  factory BookableSlotModel.fromJson(Map<String, dynamic> json) =>
      _$BookableSlotModelFromJson(json);
}
