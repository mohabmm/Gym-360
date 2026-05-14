import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

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
    parser: (data) => _asList(data)
        .map((e) => ConversationModel.fromJson(e as Map<String, dynamic>))
        .toList(),
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
    ApiEndpoints.messages(conversationId),
    queryParameters: {
      'page': 1,
      'pageSize': 100,
    },
    parser: (data) => _asList(data)
        .map((e) => MessageModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});
