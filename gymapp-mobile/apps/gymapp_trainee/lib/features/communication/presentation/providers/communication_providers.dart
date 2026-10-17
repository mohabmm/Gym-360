import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/communication_remote_datasource.dart';
import '../../data/datasources/notifications_remote_datasource.dart';
import '../../data/repositories/communication_repository.dart';
import '../../data/repositories/notifications_repository.dart';

final communicationRemoteDataSourceProvider =
    Provider<CommunicationRemoteDataSource>((ref) {
  return CommunicationRemoteDataSource(
      apiClient: ref.watch(apiClientProvider));
});

final communicationRepositoryProvider =
    Provider<CommunicationRepository>((ref) {
  return CommunicationRepository(
    remoteDataSource: ref.watch(communicationRemoteDataSourceProvider),
  );
});

final notificationsRemoteDataSourceProvider =
    Provider<NotificationsRemoteDataSource>((ref) {
  return NotificationsRemoteDataSource(
      apiClient: ref.watch(apiClientProvider));
});

final notificationsRepositoryProvider =
    Provider<NotificationsRepository>((ref) {
  return NotificationsRepository(
    remoteDataSource: ref.watch(notificationsRemoteDataSourceProvider),
  );
});

final conversationsProvider =
    FutureProvider<List<ConversationModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) return const [];
  final repo = ref.watch(communicationRepositoryProvider);
  return repo.getConversations(userId: userId, gymId: gymId);
});

final messagesProvider =
    FutureProvider.family<List<MessageModel>, String>(
        (ref, conversationId) async {
  final repo = ref.watch(communicationRepositoryProvider);
  return repo.getMessages(conversationId);
});

final announcementsProvider =
    FutureProvider<List<AnnouncementModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final gymId = authState?.currentGymId;
  if (gymId == null) return const [];
  final repo = ref.watch(communicationRepositoryProvider);
  return repo.getAnnouncements(gymId: gymId);
});

final notificationsProvider =
    FutureProvider<List<NotificationModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  if (authState?.user == null) return const [];
  final gymId = authState?.currentGymId;
  final repo = ref.watch(notificationsRepositoryProvider);
  return repo.getNotifications(gymId: gymId);
});

/// Unread notification count for the bell badge. Caller derived from JWT.
final unreadNotificationCountProvider = FutureProvider<int>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  if (authState?.user == null) return 0;
  final gymId = authState?.currentGymId;
  final repo = ref.watch(notificationsRepositoryProvider);
  return repo.getUnreadNotificationCount(gymId: gymId);
});

final notificationPreferencesProvider =
    FutureProvider<NotificationPreferenceModel>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  if (authState?.user == null) {
    throw Exception('No authenticated user');
  }
  final gymId = authState?.currentGymId;
  final repo = ref.watch(notificationsRepositoryProvider);
  return repo.getNotificationPreferences(gymId: gymId);
});

/// Active conversation ID for the chat screen
final activeConversationIdProvider = StateProvider<String?>((ref) => null);

/// Session-scoped SignalR chat hub. The backend fans chat events out only via
/// `/hubs/chat` (IChatNotifier) — there is no FCM push on message send — so
/// live chat must ride SignalR (the app already uses SignalR for branding).
/// Connects once per authenticated session; screens listen to its streams.
final chatRealtimeProvider = Provider<ChatRealtime?>((ref) {
  final auth = ref.watch(authProvider).valueOrNull;
  if (auth?.status != AuthStatus.authenticated) return null;

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

/// Emits every incoming FCM data message. Notifications are delivered via FCM
/// (the CreateNotification → IPushNotificationService path), so the bell badge
/// and the notifications list listen to this to refresh live.
final incomingNotificationProvider =
    StreamProvider.autoDispose<Map<String, dynamic>>((ref) {
  final fcm = ref.watch(fcmServiceProvider);
  return fcm.onMessage;
});
