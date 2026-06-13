import 'package:freezed_annotation/freezed_annotation.dart';

part 'notification_model.freezed.dart';
part 'notification_model.g.dart';

@freezed
class NotificationModel with _$NotificationModel {
  const factory NotificationModel({
    required String id,
    required String userId,
    required String title,
    String? titleAr,
    required String body,
    String? bodyAr,
    required String type,
    String? referenceId,
    String? referenceType,
    Map<String, dynamic>? data,
    @Default(false) bool isRead,
    DateTime? readAt,
    required DateTime createdAt,
  }) = _NotificationModel;

  factory NotificationModel.fromJson(Map<String, dynamic> json) =>
      _$NotificationModelFromJson(json);
}

@freezed
class NotificationPreferenceModel with _$NotificationPreferenceModel {
  const factory NotificationPreferenceModel({
    required String id,
    required String userId,
    @Default(true) bool sessionReminders,
    @Default(true) bool expiryWarnings,
    @Default(true) bool announcements,
    @Default(true) bool chatMessages,
    @Default(true) bool promotions,
    @Default(true) bool workoutReminders,
    @Default(true) bool progressReminders,
  }) = _NotificationPreferenceModel;

  factory NotificationPreferenceModel.fromJson(Map<String, dynamic> json) =>
      _$NotificationPreferenceModelFromJson(json);
}

/// FCM push notification types
class NotificationType {
  NotificationType._();

  static const String configRefresh = 'config_refresh';
  static const String gymSuspended = 'gym_suspended';
  static const String gymReactivated = 'gym_reactivated';
  static const String sessionReminder = 'session_reminder';
  static const String sessionBooked = 'session_booked';
  static const String sessionCancelled = 'session_cancelled';
  static const String newMessage = 'new_message';
  static const String announcement = 'announcement';
  static const String membershipExpiring = 'membership_expiring';
  static const String membershipExpired = 'membership_expired';
  static const String packageLow = 'package_low';
  static const String packageExhausted = 'package_exhausted';
  static const String workoutAssigned = 'workout_assigned';
  static const String rateTrainer = 'rate_trainer';
  static const String bookingRequest = 'booking_request';
  static const String bookingAccepted = 'booking_accepted';
  static const String bookingRejected = 'booking_rejected';
  static const String paymentRecorded = 'payment_recorded';
  static const String graceWarning = 'grace_warning';
}
