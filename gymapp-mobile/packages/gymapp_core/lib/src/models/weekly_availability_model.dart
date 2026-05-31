import 'package:freezed_annotation/freezed_annotation.dart';

part 'weekly_availability_model.freezed.dart';
part 'weekly_availability_model.g.dart';

enum WeekSlotStatus {
  @JsonValue('Available')
  available,
  @JsonValue('BookedByMe')
  bookedByMe,
  @JsonValue('BookedByOther')
  bookedByOther,
}

@freezed
class WeeklyAvailabilityModel with _$WeeklyAvailabilityModel {
  const factory WeeklyAvailabilityModel({
    required String trainerId,
    required String gymId,
    required DateTime startDate,
    required DateTime endDate,
    required int sessionDurationMinutes,
    required int bufferMinutes,
    required List<DayAvailabilityModel> days,
  }) = _WeeklyAvailabilityModel;

  factory WeeklyAvailabilityModel.fromJson(Map<String, dynamic> json) =>
      _$WeeklyAvailabilityModelFromJson(json);
}

@freezed
class DayAvailabilityModel with _$DayAvailabilityModel {
  const factory DayAvailabilityModel({
    required DateTime date,
    required String dayOfWeek,
    required bool isOffDay,
    required bool isBlocked,
    String? blockedReason,
    required List<WeekSlotModel> slots,
  }) = _DayAvailabilityModel;

  factory DayAvailabilityModel.fromJson(Map<String, dynamic> json) =>
      _$DayAvailabilityModelFromJson(json);
}

@freezed
class WeekSlotModel with _$WeekSlotModel {
  const factory WeekSlotModel({
    required String startTime,
    required String endTime,
    required WeekSlotStatus status,
    String? sessionId,
  }) = _WeekSlotModel;

  factory WeekSlotModel.fromJson(Map<String, dynamic> json) =>
      _$WeekSlotModelFromJson(json);
}
