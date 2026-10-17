import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// Session-scoped SignalR chat hub. The backend fans chat events out only via
/// `/hubs/chat` (IChatNotifier) — there is no FCM push on message send — so
/// live chat must ride SignalR (the app already uses SignalR for branding).
/// Connects once per authenticated session; screens listen to its streams.
final chatRealtimeProvider = Provider<ChatRealtime?>((ref) {
  final auth = ref.watch(authProvider);
  if (auth.status != AuthStatus.authenticated) return null;

  final realtime = ChatRealtime();
  final api = ref.read(apiClientProvider);
  final storage = ref.read(secureStorageProvider);

  () async {
    final token = await storage.getAccessToken();
    if (token == null) return;
    await realtime.connect(api, token);
  }();

  ref.onDispose(realtime.dispose);
  return realtime;
});

/// Emits when a `message_received` event arrives on the chat hub.
final incomingChatMessageProvider =
    StreamProvider.autoDispose<Map<String, dynamic>>((ref) {
  final realtime = ref.watch(chatRealtimeProvider);
  if (realtime == null) return const Stream.empty();
  return realtime.onMessageReceived;
});

/// Emits when a `messages_read` event arrives — lets a sender flip ✓ → ✓✓ live.
final chatMessagesReadProvider =
    StreamProvider.autoDispose<Map<String, dynamic>>((ref) {
  final realtime = ref.watch(chatRealtimeProvider);
  if (realtime == null) return const Stream.empty();
  return realtime.onMessagesRead;
});

List<dynamic> _asList(dynamic data) {
  if (data is List) return data;
  if (data is Map<String, dynamic> && data['items'] is List) {
    return data['items'] as List<dynamic>;
  }
  return const [];
}

/// All conversations for this trainer.
/// BE requires userId, gymId, page, pageSize as query parameters.
final conversationsProvider =
    FutureProvider.autoDispose<List<ConversationModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final userId = ref.watch(authProvider).user?.id;
  final gymId = ref.watch(activeGymIdProvider);
  if (userId == null || gymId == null) return <ConversationModel>[];

  // userId + gymId are derived server-side from JWT claims + X-Gym-Id header.
  return apiClient.get<List<ConversationModel>>(
    ApiEndpoints.conversations,
    queryParameters: {
      'page': 1,
      'pageSize': 50,
    },
    parser: (data) {
      final list = _asList(data)
          .map((e) => ConversationModel.fromJson(e as Map<String, dynamic>))
          .toList();
      // Most-recent conversation first. Fall back to updatedAt when a
      // conversation has no messages yet (lastMessageAt == null).
      list.sort((a, b) {
        final ad = a.lastMessageAt ?? a.updatedAt;
        final bd = b.lastMessageAt ?? b.updatedAt;
        return bd.compareTo(ad);
      });
      return list;
    },
  );
});

/// Resolves the conversation title to the counterpart participant's name.
/// Falls back to a generic "Conversation" label when the conversation list
/// hasn't loaded yet or the conversation can't be found.
final conversationTitleProvider =
    Provider.autoDispose.family<String?, String>((ref, conversationId) {
  final currentUserId = ref.watch(authProvider).user?.id;
  final conversationsAsync = ref.watch(conversationsProvider);
  return conversationsAsync.maybeWhen(
    data: (conversations) {
      final match = conversations.where((c) => c.id == conversationId);
      if (match.isEmpty) return null;
      final others = match.first.participants
          .where((p) => p.userId != currentUserId)
          .toList();
      if (others.isEmpty) return null;
      return others.map((p) => p.name).join(', ');
    },
    orElse: () => null,
  );
});

/// Messages in a conversation.
/// BE requires userId, page, pageSize as query parameters.
final conversationMessagesProvider =
    FutureProvider.autoDispose.family<List<MessageModel>, String>(
        (ref, conversationId) async {
  final apiClient = ref.watch(apiClientProvider);
  final userId = ref.watch(authProvider).user?.id;
  if (userId == null) return <MessageModel>[];

  return apiClient.get<List<MessageModel>>(
    ApiEndpoints.messages(ConversationId(conversationId)),
    queryParameters: {
      'page': 1,
      'pageSize': 100,
    },
    parser: (data) {
      final list = _asList(data)
          .map((e) => MessageModel.fromJson(e as Map<String, dynamic>))
          .toList();
      // Oldest first so the thread reads top→bottom and the newest message
      // sits at the bottom (where the view auto-scrolls).
      list.sort((a, b) => a.sentAt.compareTo(b.sentAt));
      return list;
    },
  );
});

// ---------------------------------------------------------------------------
// Notifications — caller derived from JWT server-side.
// ---------------------------------------------------------------------------

/// Emits every incoming FCM data message. Notifications ride FCM (the
/// CreateNotification → IPushNotificationService path), so the bell badge and
/// the inbox listen to this to refresh live.
final incomingNotificationProvider =
    StreamProvider.autoDispose<Map<String, dynamic>>((ref) {
  final fcm = ref.watch(fcmServiceProvider);
  return fcm.onMessage;
});

/// The trainer's notifications (newest first from the BE).
final notificationsProvider =
    FutureProvider.autoDispose<List<NotificationModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final userId = ref.watch(authProvider).user?.id;
  if (userId == null) return <NotificationModel>[];

  return apiClient.get<List<NotificationModel>>(
    ApiEndpoints.notifications,
    queryParameters: {'page': 1, 'pageSize': 50},
    parser: (data) => _asList(data)
        .map((e) => NotificationModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});

/// Unread notification count for the bell badge. Caller derived from JWT.
final unreadNotificationCountProvider =
    FutureProvider.autoDispose<int>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final userId = ref.watch(authProvider).user?.id;
  if (userId == null) return 0;

  return apiClient.get<int>(
    ApiEndpoints.notificationsUnreadCount,
    parser: (data) {
      if (data is int) return data;
      if (data is Map) {
        if (data['unreadNotifications'] is int) {
          return data['unreadNotifications'] as int;
        }
        if (data['count'] is int) return data['count'] as int;
      }
      return 0;
    },
  );
});

/// Mark a single notification read (caller from JWT), then refresh list+badge.
Future<void> markTrainerNotificationRead(WidgetRef ref, String id) async {
  final apiClient = ref.read(apiClientProvider);
  try {
    await apiClient.putVoid(ApiEndpoints.notificationMarkRead(NotificationId(id)));
  } catch (_) {
    // Non-critical.
  }
  ref.invalidate(notificationsProvider);
  ref.invalidate(unreadNotificationCountProvider);
}

/// Mark all notifications read (caller from JWT), then refresh list+badge.
Future<void> markAllTrainerNotificationsRead(WidgetRef ref) async {
  final apiClient = ref.read(apiClientProvider);
  try {
    await apiClient.putVoid(ApiEndpoints.notificationsReadAll);
  } catch (_) {
    // Non-critical.
  }
  ref.invalidate(notificationsProvider);
  ref.invalidate(unreadNotificationCountProvider);
}
