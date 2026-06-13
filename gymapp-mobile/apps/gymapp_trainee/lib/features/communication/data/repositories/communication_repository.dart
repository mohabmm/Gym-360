import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/communication_remote_datasource.dart';

class CommunicationRepository {
  final CommunicationRemoteDataSource _remoteDataSource;

  CommunicationRepository(
      {required CommunicationRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<List<ConversationModel>> getConversations({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getConversations(userId: userId, gymId: gymId);

  Future<List<MessageModel>> getMessages(String conversationId) =>
      _remoteDataSource.getMessages(conversationId);

  /// Returns the message ID (Guid string). Sender derived from JWT.
  Future<String> sendMessage({
    required String conversationId,
    required String content,
    String? mediaUrl,
    String? mediaType,
  }) =>
      _remoteDataSource.sendMessage(
        conversationId: conversationId,
        content: content,
        mediaUrl: mediaUrl,
        mediaType: mediaType,
      );

  Future<void> markMessagesRead({required String conversationId}) =>
      _remoteDataSource.markMessagesRead(conversationId: conversationId);

  Future<List<AnnouncementModel>> getAnnouncements({
    required String gymId,
  }) =>
      _remoteDataSource.getAnnouncements(gymId: gymId);

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
