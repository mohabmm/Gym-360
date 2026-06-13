import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/communication_providers.dart';

class ChatListScreen extends ConsumerWidget {
  const ChatListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final conversationsAsync = ref.watch(conversationsProvider);
    final auth = ref.watch(authProvider);

    // Refresh the list (unread badges, last-message preview) when a new_message
    // push arrives while the list is open.
    ref.listen(incomingChatMessageProvider, (_, __) {
      ref.invalidate(conversationsProvider);
    });

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'المحادثات' : 'Messages'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(conversationsProvider),
        child: conversationsAsync.when(
          data: (conversations) {
            if (conversations.isEmpty) {
              return EmptyState(
                icon: Icons.chat_outlined,
                title: isArabic ? 'لا توجد محادثات' : 'No conversations',
                subtitle: isArabic
                    ? 'ستظهر محادثاتك مع العملاء هنا'
                    : 'Your client conversations will appear here',
              );
            }

            return ListView.builder(
              itemCount: conversations.length,
              itemBuilder: (context, index) {
                final conv = conversations[index];
                final myId = auth.user?.id ?? '';
                final other = conv.participants.firstWhere(
                  (p) => p.userId != myId,
                  orElse: () => conv.participants.isNotEmpty
                      ? conv.participants.first
                      : const ConversationParticipantModel(
                          userId: '',
                          name: '',
                          role: '',
                        ),
                );
                final displayName = other.name.isNotEmpty
                    ? other.name
                    : (isArabic ? 'محادثة' : 'Conversation');
                final hasUnread = conv.unreadCount > 0;

                return ListTile(
                  leading: AvatarWidget(
                    imageUrl: other.photoUrl,
                    name: displayName,
                    size: AppConstants.avatarSizeMedium,
                  ),
                  title: Text(
                    displayName,
                    style: TextStyle(
                      fontWeight:
                          hasUnread ? FontWeight.w700 : FontWeight.w400,
                    ),
                  ),
                  subtitle: conv.lastMessage != null
                      ? Text(
                          conv.lastMessage!.content,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontWeight: hasUnread
                                ? FontWeight.w600
                                : FontWeight.w400,
                          ),
                        )
                      : null,
                  trailing: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      if (conv.lastMessage != null)
                        Text(
                          DateFormat('hh:mm a', isArabic ? 'ar' : 'en')
                              .format(conv.lastMessage!.sentAt),
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      if (hasUnread)
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 6, vertical: 2),
                          decoration: BoxDecoration(
                            color: Theme.of(context).colorScheme.primary,
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: Text(
                            '${conv.unreadCount}',
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                    ],
                  ),
                  onTap: () => context.push('/more/chat/${conv.id}'),
                );
              },
            );
          },
          loading: () => ListView.builder(
            itemCount: 6,
            itemBuilder: (_, __) => const Padding(
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
              child: ShimmerCard(height: 60),
            ),
          ),
          error: (e, _) => AppErrorWidget(
            message: isArabic
                ? 'تعذر تحميل المحادثات. يرجى المحاولة مرة أخرى.'
                : 'Could not load conversations. Please try again.',
            onRetry: () => ref.invalidate(conversationsProvider),
          ),
        ),
      ),
    );
  }
}
