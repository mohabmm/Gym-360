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
    required MessageDraft draft,
  }) =>
      _remoteDataSource.sendMessage(
        conversationId: conversationId,
        draft: draft,
      );

  Future<void> markMessagesRead({required String conversationId}) =>
      _remoteDataSource.markMessagesRead(conversationId: conversationId);

  Future<List<AnnouncementModel>> getAnnouncements({
    required String gymId,
  }) =>
      _remoteDataSource.getAnnouncements(gymId: gymId);
}
