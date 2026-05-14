import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/communication_remote_datasource.dart';
import '../../data/repositories/communication_repository.dart';

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
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null) return const [];
  final repo = ref.watch(communicationRepositoryProvider);
  return repo.getNotifications(userId: userId, gymId: gymId);
});

final notificationPreferencesProvider =
    FutureProvider<NotificationPreferenceModel>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null) {
    throw Exception('No authenticated user');
  }
  final repo = ref.watch(communicationRepositoryProvider);
  return repo.getNotificationPreferences(userId: userId, gymId: gymId);
});

/// Active conversation ID for the chat screen
final activeConversationIdProvider = StateProvider<String?>((ref) => null);
