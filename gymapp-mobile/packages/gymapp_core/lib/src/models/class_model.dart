import 'package:freezed_annotation/freezed_annotation.dart';

part 'class_model.freezed.dart';
part 'class_model.g.dart';

// Mirrors the backend `ClassScheduleDto`. BE serialises [dayOfWeek] as the
// lowercase enum name (e.g. "monday") and [startTime] as "HH:mm:ss".
@freezed
class ClassScheduleModel with _$ClassScheduleModel {
  const factory ClassScheduleModel({
    @Default('') String dayOfWeek,
    @Default('') String startTime,
    @Default(60) int durationMinutes,
    @Default(false) bool isRecurring,
  }) = _ClassScheduleModel;

  factory ClassScheduleModel.fromJson(Map<String, dynamic> json) =>
      _$ClassScheduleModelFromJson(json);
}

// Mirrors backend `GymClassDto`. BE exposes the session's weekly slot via
// nested [schedule] (dayOfWeek + startTime string + durationMinutes), not flat
// DateTime start/end. [trainerName] replaces the old [instructorName] field.
@freezed
class GymClassModel with _$GymClassModel {
  const factory GymClassModel({
    required String id,
    required String gymId,
    required String name,
    String? nameAr,
    String? description,
    String? descriptionAr,
    String? trainerId,
    String? trainerName,
    @Default(0) int capacity,
    @Default(0) int bookedCount,
    int? waitlistCount,
    @Default(true) bool waitlistEnabled,
    ClassScheduleModel? schedule,
    String? location,
    String? section,
    @Default('Active') String status,
    String? imageUrl,
    String? category,
    DateTime? createdAt,
  }) = _GymClassModel;

  factory GymClassModel.fromJson(Map<String, dynamic> json) =>
      _$GymClassModelFromJson(json);
}

@freezed
class ClassBookingModel with _$ClassBookingModel {
  const factory ClassBookingModel({
    required String id,
    required String classId,
    required String memberId,
    String? memberName,
    String? memberPhotoUrl,
    required String status,
    int? waitlistPosition,
    DateTime? bookedAt,
    DateTime? cancelledAt,
    @Default(false) bool attended,
  }) = _ClassBookingModel;

  factory ClassBookingModel.fromJson(Map<String, dynamic> json) =>
      _$ClassBookingModelFromJson(json);
}

@freezed
class CreateClassRequest with _$CreateClassRequest {
  const factory CreateClassRequest({
    required String name,
    String? nameAr,
    String? description,
    String? descriptionAr,
    required String instructorName,
    String? instructorId,
    required int capacity,
    required DateTime startTime,
    required DateTime endTime,
    String? location,
    String? section,
    String? category,
    @Default(false) bool isRecurring,
    int? recurringDayOfWeek,
  }) = _CreateClassRequest;

  factory CreateClassRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateClassRequestFromJson(json);
}
