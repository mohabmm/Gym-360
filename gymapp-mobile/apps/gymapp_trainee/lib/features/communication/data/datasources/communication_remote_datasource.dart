import 'package:gymapp_core/gymapp_core.dart';

import 'paged_items.dart';

class CommunicationRemoteDataSource {
  final ApiClient _apiClient;

  CommunicationRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE GET /communication/conversations requires userId, gymId, page, pageSize.
  Future<List<ConversationModel>> getConversations({
    required String userId,
    required String gymId,
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<ConversationModel>>(
      ApiEndpoints.conversations,
      queryParameters: {
        'userId': userId,
        'gymId': gymId,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) {
        final list = unwrapItems(data)
            .map<ConversationModel>(
                (e) => ConversationModel.fromJson(e as Map<String, dynamic>))
            .toList();
        // Most-recent conversation first.
        list.sort((a, b) {
          final ad = a.lastMessageAt ?? a.updatedAt;
          final bd = b.lastMessageAt ?? b.updatedAt;
          return bd.compareTo(ad);
        });
        return list;
      },
    );
  }

  /// BE GET /communication/conversations/{id}/messages — caller derived from JWT.
  Future<List<MessageModel>> getMessages(
    String conversationId, {
    int page = 1,
    int pageSize = 50,
  }) async {
    return _apiClient.get<List<MessageModel>>(
      ApiEndpoints.messages(ConversationId(conversationId)),
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) {
        final list = unwrapItems(data)
            .map<MessageModel>(
                (e) => MessageModel.fromJson(e as Map<String, dynamic>))
            .toList();
        // Oldest first so newest sits at the bottom of the thread.
        list.sort((a, b) => a.sentAt.compareTo(b.sentAt));
        return list;
      },
    );
  }

  /// BE POST /communication/conversations/{id}/messages — sender derived from JWT.
  Future<String> sendMessage({
    required String conversationId,
    required MessageDraft draft,
  }) async {
    return _apiClient.post<String>(
      ApiEndpoints.messages(ConversationId(conversationId)),
      data: draft.toJson(),
      parser: (data) => data.toString(),
    );
  }

  /// BE PUT /communication/conversations/{conversationId}/read — user derived from JWT.
  Future<void> markMessagesRead({
    required String conversationId,
  }) async {
    await _apiClient.putVoid(
      '${ApiEndpoints.conversationById(ConversationId(conversationId))}/read',
    );
  }

  /// BE GET /gyms/{gymId}/announcements — requires gymId in URL path.
  Future<List<AnnouncementModel>> getAnnouncements({
    required String gymId,
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<AnnouncementModel>>(
      '${ApiEndpoints.gyms}/$gymId/announcements',
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => unwrapItems(data)
          .map<AnnouncementModel>((e) => AnnouncementModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
