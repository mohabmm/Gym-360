import 'package:gymapp_core/gymapp_core.dart';

import 'paged_items.dart';

/// Notifications + notification preferences. Caller derived from JWT.
class NotificationsRemoteDataSource {
  final ApiClient _apiClient;

  NotificationsRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE GET /communication/notifications — caller derived from JWT.
  Future<List<NotificationModel>> getNotifications({
    String? gymId,
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<NotificationModel>>(
      ApiEndpoints.notifications,
      queryParameters: {
        if (gymId != null) 'gymId': gymId,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => unwrapItems(data)
          .map<NotificationModel>((e) => NotificationModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// BE PUT /communication/notifications/{id}/read — caller derived from JWT.
  Future<void> markNotificationRead(String id) async {
    await _apiClient.putVoid(ApiEndpoints.notificationMarkRead(id));
  }

  /// BE PUT /communication/notifications/read-all — caller derived from JWT.
  Future<void> markAllNotificationsRead() async {
    await _apiClient.putVoid(ApiEndpoints.notificationsReadAll);
  }

  /// BE GET /communication/notifications/unread-count — caller derived from JWT.
  /// Response: { unreadNotifications, unreadMessages }. Returns unread
  /// notifications count (bell badge); tolerant of legacy/int shapes.
  Future<int> getUnreadNotificationCount({String? gymId}) async {
    return _apiClient.get<int>(
      ApiEndpoints.notificationsUnreadCount,
      queryParameters: {
        if (gymId != null) 'gymId': gymId,
      },
      parser: _unreadCount,
    );
  }

  /// BE GET /communication/notification-preferences — caller derived from JWT.
  Future<NotificationPreferenceModel> getNotificationPreferences({
    String? gymId,
  }) async {
    return _apiClient.get<NotificationPreferenceModel>(
      ApiEndpoints.notificationPreferences,
      queryParameters: {
        if (gymId != null) 'gymId': gymId,
      },
      parser: (data) => NotificationPreferenceModel.fromJson(
          data as Map<String, dynamic>),
    );
  }

  /// BE PUT /communication/notification-preferences expects userId, gymId,
  /// preferences list, quietHoursStart, quietHoursEnd.
  Future<void> updateNotificationPreferences(
      Map<String, dynamic> body) async {
    await _apiClient.putVoid(
      ApiEndpoints.notificationPreferences,
      data: body,
    );
  }
}

/// Keys checked, in order, for the unread notifications count.
const _unreadCountKeys = ['unreadNotifications', 'notifications', 'count'];

int _unreadCount(dynamic data) {
  if (data is int) return data;
  if (data is! Map) return 0;
  final count = _unreadCountKeys
      .map((key) => data[key])
      .firstWhere((value) => value is int, orElse: () => 0);
  return count as int;
}
