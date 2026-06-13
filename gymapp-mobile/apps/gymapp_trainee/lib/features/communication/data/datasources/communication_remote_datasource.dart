import 'package:gymapp_core/gymapp_core.dart';

List<dynamic> _unwrapItems(dynamic data) {
  if (data is List) return data;
  if (data is Map<String, dynamic> && data['items'] is List) {
    return data['items'] as List<dynamic>;
  }
  return const [];
}

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
        final list = _unwrapItems(data)
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
      ApiEndpoints.messages(conversationId),
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) {
        final list = _unwrapItems(data)
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
    required String content,
    String? mediaUrl,
    String? mediaType,
  }) async {
    return _apiClient.post<String>(
      ApiEndpoints.messages(conversationId),
      data: {
        'content': content,
        if (mediaUrl != null) 'mediaUrl': mediaUrl,
        if (mediaType != null) 'mediaType': mediaType,
      },
      parser: (data) => data.toString(),
    );
  }

  /// BE PUT /communication/conversations/{conversationId}/read — user derived from JWT.
  Future<void> markMessagesRead({
    required String conversationId,
  }) async {
    await _apiClient.putVoid(
      '${ApiEndpoints.conversationById(conversationId)}/read',
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
      parser: (data) => _unwrapItems(data)
          .map<AnnouncementModel>((e) => AnnouncementModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

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
      parser: (data) => _unwrapItems(data)
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
      parser: (data) {
        if (data is int) return data;
        if (data is Map) {
          if (data['unreadNotifications'] is int) {
            return data['unreadNotifications'] as int;
          }
          if (data['notifications'] is int) return data['notifications'] as int;
          if (data['count'] is int) return data['count'] as int;
        }
        return 0;
      },
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
