import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/communication_providers.dart';

class ChatScreen extends ConsumerWidget {
  const ChatScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final conversationsAsync = ref.watch(conversationsProvider);
    final activeConversationId = ref.watch(activeConversationIdProvider);

    if (activeConversationId != null) {
      return _ConversationScreen(
        conversationId: activeConversationId,
        isArabic: isArabic,
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'المحادثات' : 'Chat'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(conversationsProvider),
        child: conversationsAsync.when(
          data: (conversations) {
            if (conversations.isEmpty) {
              return EmptyState(
                icon: Icons.chat_outlined,
                title:
                    isArabic ? 'لا توجد محادثات' : 'No conversations',
                subtitle: isArabic
                    ? 'محادثاتك مع المدرب ستظهر هنا'
                    : 'Your trainer conversations will appear here',
              );
            }
            return ListView.builder(
              itemCount: conversations.length,
              itemBuilder: (context, index) => _ConversationTile(
                conversation: conversations[index],
                isArabic: isArabic,
              ),
            );
          },
          loading: () =>
              const Center(child: CircularProgressIndicator()),
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

class _ConversationTile extends ConsumerWidget {
  final ConversationModel conversation;
  final bool isArabic;

  const _ConversationTile(
      {required this.conversation, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final myId = authState.valueOrNull?.user?.id ?? '';
    final other = conversation.participants.firstWhere(
      (p) => p.userId != myId,
      orElse: () => conversation.participants.isNotEmpty
          ? conversation.participants.first
          : const ConversationParticipantModel(
              userId: '',
              name: '',
              role: '',
            ),
    );
    final displayName = other.name.isNotEmpty
        ? other.name
        : (isArabic ? 'محادثة' : 'Conversation');
    final hasUnread = conversation.unreadCount > 0;

    return ListTile(
      leading: AvatarWidget(
        imageUrl: other.photoUrl,
        name: displayName,
        size: 48,
      ),
      title: Text(displayName,
          style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: conversation.lastMessage != null
          ? Text(
              conversation.lastMessage!.content,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            )
          : null,
      trailing: hasUnread
          ? Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.primary,
                shape: BoxShape.circle,
              ),
              child: Text(
                '${conversation.unreadCount}',
                style: const TextStyle(color: Colors.white, fontSize: 12),
              ),
            )
          : null,
      onTap: () {
        ref.read(activeConversationIdProvider.notifier).state =
            conversation.id;
      },
    );
  }
}

class _ConversationScreen extends ConsumerStatefulWidget {
  final String conversationId;
  final bool isArabic;

  const _ConversationScreen(
      {required this.conversationId, required this.isArabic});

  @override
  ConsumerState<_ConversationScreen> createState() =>
      _ConversationScreenState();
}

class _ConversationScreenState
    extends ConsumerState<_ConversationScreen> {
  final _messageController = TextEditingController();
  final _scrollController = ScrollController();
  bool _isSending = false;

  @override
  void initState() {
    super.initState();
    // Mark messages as read when opening the conversation
    _markAsRead();
  }

  Future<void> _markAsRead() async {
    try {
      final repo = ref.read(communicationRepositoryProvider);
      await repo.markMessagesRead(conversationId: widget.conversationId);
      // Refresh conversation list to update unread counts
      ref.invalidate(conversationsProvider);
    } catch (_) {
      // Silent fail — marking read is non-critical
    }
  }

  @override
  void dispose() {
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    final messagesAsync =
        ref.watch(messagesProvider(widget.conversationId));
    final conversationsAsync = ref.watch(conversationsProvider);
    final myId = ref.watch(authProvider).valueOrNull?.user?.id ?? '';
    final counterpartName = conversationsAsync.maybeWhen(
      data: (conversations) {
        ConversationModel? convo;
        for (final c in conversations) {
          if (c.id == widget.conversationId) {
            convo = c;
            break;
          }
        }
        if (convo == null) return '';
        final others =
            convo.participants.where((p) => p.userId != myId).toList();
        if (others.isNotEmpty) return others.first.name;
        return convo.participants.isNotEmpty
            ? convo.participants.first.name
            : '';
      },
      orElse: () => '',
    );
    final titleText = counterpartName.isNotEmpty
        ? counterpartName
        : (isArabic ? 'المحادثة' : 'Chat');

    return Scaffold(
      appBar: AppBar(
        title: Text(titleText),
        leading: BackButton(
          onPressed: () {
            ref.read(activeConversationIdProvider.notifier).state = null;
          },
        ),
      ),
      body: Column(
        children: [
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async =>
                  ref.invalidate(messagesProvider(widget.conversationId)),
              child: messagesAsync.when(
                data: (messages) {
                  if (messages.isEmpty) {
                    return EmptyState(
                      icon: Icons.chat_bubble_outline,
                      title: isArabic
                          ? 'لا توجد رسائل بعد'
                          : 'No messages yet',
                      subtitle: isArabic
                          ? 'ابدأ المحادثة مع مدربك'
                          : 'Start the conversation with your trainer',
                    );
                  }
                  return ListView.builder(
                    controller: _scrollController,
                    padding: const EdgeInsets.all(16),
                    itemCount: messages.length,
                    itemBuilder: (context, index) {
                      final msg = messages[index];
                      final authState = ref.read(authProvider);
                      final myId = authState.valueOrNull?.user?.id;
                      final isMe = msg.senderId == myId;
                      return _MessageBubble(
                          message: msg, isMe: isMe);
                    },
                  );
                },
                loading: () =>
                    const Center(child: CircularProgressIndicator()),
                error: (e, _) => AppErrorWidget(
                  message: isArabic
                      ? 'تعذر تحميل الرسائل. يرجى المحاولة مرة أخرى.'
                      : 'Could not load messages. Please try again.',
                  onRetry: () => ref.invalidate(
                      messagesProvider(widget.conversationId)),
                ),
              ),
            ),
          ),

          // Input — wrapped in SafeArea so the compose row clears the
          // outer shell's bottom nav while the keyboard is open (BUG-10).
          SafeArea(
            top: false,
            child: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surface,
                border: Border(
                  top: BorderSide(
                      color: Theme.of(context).colorScheme.outline),
                ),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _messageController,
                      decoration: InputDecoration(
                        hintText: isArabic
                            ? 'اكتب رسالتك...'
                            : 'Type a message...',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                        ),
                        contentPadding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 8),
                      ),
                      maxLines: null,
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton.filled(
                    onPressed: _isSending ? null : _sendMessage,
                    tooltip: isArabic ? 'إرسال' : 'Send',
                    // Mirrored on Icon.semanticLabel so VoiceOver reads it
                    // even when tooltips aren't surfaced.
                    icon: _isSending
                        ? const SizedBox(
                            height: 16,
                            width: 16,
                            child: CircularProgressIndicator(
                                strokeWidth: 2, color: Colors.white))
                        : Icon(Icons.send,
                            semanticLabel: isArabic ? 'إرسال' : 'Send'),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _sendMessage() async {
    final content = _messageController.text.trim();
    if (content.isEmpty) return;

    setState(() => _isSending = true);
    _messageController.clear();

    try {
      // Sender derived from JWT — server no longer accepts senderId in body.
      final repo = ref.read(communicationRepositoryProvider);
      await repo.sendMessage(
        conversationId: widget.conversationId,
        content: content,
      );
      ref.invalidate(messagesProvider(widget.conversationId));
      ref.invalidate(conversationsProvider);
    } catch (_) {
      if (mounted) {
        final isArabic =
            Localizations.localeOf(context).languageCode == 'ar';
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'تعذر إرسال الرسالة. يرجى المحاولة مرة أخرى.'
              : 'Could not send the message. Please try again.'),
        ));
      }
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }
}

/// Timestamp string for a chat bubble. Today's messages show just "HH:mm a"
/// so they stay compact; anything older carries an explicit date so the
/// conversation doesn't ambiguously read "04:45" forever (BUG-10).
String _formatSentAt(BuildContext context, DateTime sentAt) {
  final now = DateTime.now();
  final isToday = now.year == sentAt.year &&
      now.month == sentAt.month &&
      now.day == sentAt.day;
  final isArabic = Localizations.localeOf(context).languageCode == 'ar';
  final locale = isArabic ? 'ar' : 'en';
  final local = sentAt.toLocal();
  if (isToday) {
    return DateFormat('h:mm a', locale).format(local);
  }
  return DateFormat('MMM d, h:mm a', locale).format(local);
}

class _MessageBubble extends StatelessWidget {
  final MessageModel message;
  final bool isMe;

  const _MessageBubble({required this.message, required this.isMe});

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: isMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 4),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.7,
        ),
        decoration: BoxDecoration(
          color: isMe
              ? Theme.of(context).colorScheme.primary
              : Theme.of(context).colorScheme.surfaceVariant,
          borderRadius: BorderRadius.only(
            topLeft: const Radius.circular(16),
            topRight: const Radius.circular(16),
            bottomLeft: Radius.circular(isMe ? 16 : 4),
            bottomRight: Radius.circular(isMe ? 4 : 16),
          ),
        ),
        child: Column(
          crossAxisAlignment:
              isMe ? CrossAxisAlignment.end : CrossAxisAlignment.start,
          children: [
            Text(
              message.content,
              style: TextStyle(
                color: isMe ? Colors.white : null,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              _formatSentAt(context, message.sentAt),
              style: TextStyle(
                fontSize: 11,
                color: isMe
                    ? Colors.white.withOpacity(0.7)
                    : Theme.of(context)
                        .textTheme
                        .bodySmall
                        ?.color
                        ?.withOpacity(0.7),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
