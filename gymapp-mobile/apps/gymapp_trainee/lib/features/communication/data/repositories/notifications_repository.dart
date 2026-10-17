import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/notifications_remote_datasource.dart';

class NotificationsRepository {
  final NotificationsRemoteDataSource _remoteDataSource;

  NotificationsRepository(
      {required NotificationsRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<List<NotificationModel>> getNotifications({String? gymId}) =>
      _remoteDataSource.getNotifications(gymId: gymId);

  Future<void> markNotificationRead(String id) =>
      _remoteDataSource.markNotificationRead(id);

  Future<void> markAllNotificationsRead() =>
      _remoteDataSource.markAllNotificationsRead();

  Future<int> getUnreadNotificationCount({String? gymId}) =>
      _remoteDataSource.getUnreadNotificationCount(gymId: gymId);

  Future<NotificationPreferenceModel> getNotificationPreferences({
    String? gymId,
  }) =>
      _remoteDataSource.getNotificationPreferences(gymId: gymId);

  Future<void> updateNotificationPreferences(Map<String, dynamic> body) =>
      _remoteDataSource.updateNotificationPreferences(body);
}
