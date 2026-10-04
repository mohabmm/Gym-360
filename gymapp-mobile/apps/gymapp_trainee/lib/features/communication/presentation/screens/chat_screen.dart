import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/communication_providers.dart';

/// Local prefix that marks an optimistic (not-yet-confirmed) message so the
/// bubble can render a "sending" clock instead of a delivery tick.
const _optimisticPrefix = 'optimistic-';

class ChatScreen extends ConsumerWidget {
  const ChatScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final conversationsAsync = ref.watch(conversationsProvider);
    final activeConversationId = ref.watch(activeConversationIdProvider);

    // Refresh the conversation list (unread badges, previews) when a new_message
    // push lands while the list is open.
    ref.listen(incomingChatMessageProvider, (_, __) {
      ref.invalidate(conversationsProvider);
    });

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
                title: isArabic ? 'لا توجد محادثات' : 'No conversations',
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
          loading: () => const Center(child: CircularProgressIndicator()),
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
      title: Text(
        displayName,
        style: TextStyle(
          fontWeight: hasUnread ? FontWeight.w700 : FontWeight.w600,
        ),
      ),
      subtitle: conversation.lastMessage != null
          ? Text(
              conversation.lastMessage!.content,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontWeight: hasUnread ? FontWeight.w600 : FontWeight.normal,
              ),
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

class _ConversationScreenState extends ConsumerState<_ConversationScreen> {
  final _messageController = TextEditingController();
  final _scrollController = ScrollController();
  bool _isSending = false;

  /// Cached realtime handle captured in [initState] so [dispose] can leave the
  /// SignalR group without touching `ref` — reading a provider after the
  /// element is disposed throws "Cannot use ref after the widget was disposed".
  ChatRealtime? _realtime;

  /// Optimistic messages shown immediately on send.
  final List<MessageModel> _pending = [];

  /// Auto-scroll to the bottom only on the first data render.
  bool _didInitialScroll = false;

  @override
  void initState() {
    super.initState();
    // Capture the realtime handle now; `ref` is safe in initState but not in
    // dispose, where we still need to leave the conversation group.
    _realtime = ref.read(chatRealtimeProvider);
    // Mark messages as read when opening the conversation.
    _markAsRead();
    // Join the SignalR conversation group for live message_received /
    // messages_read events.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _realtime?.joinConversation(widget.conversationId);
    });
  }

  Future<void> _markAsRead() async {
    try {
      final repo = ref.read(communicationRepositoryProvider);
      await repo.markMessagesRead(conversationId: widget.conversationId);
      ref.invalidate(conversationsProvider);
    } catch (_) {
      // Silent fail — marking read is non-critical.
    }
  }

  @override
  void dispose() {
    _realtime?.leaveConversation(widget.conversationId);
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _scrollToBottom({bool animate = false}) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!_scrollController.hasClients) return;
      final target = _scrollController.position.maxScrollExtent;
      if (animate) {
        _scrollController.animateTo(
          target,
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeOut,
        );
      } else {
        _scrollController.jumpTo(target);
      }
    });
  }

  Future<void> _sendMessage() async {
    final content = _messageController.text.trim();
    if (content.isEmpty) return;

    final myId = ref.read(authProvider).valueOrNull?.user?.id ?? '';
    final optimistic = MessageModel(
      id: '$_optimisticPrefix${DateTime.now().microsecondsSinceEpoch}',
      conversationId: widget.conversationId,
      senderId: myId,
      content: content,
      sentAt: DateTime.now(),
    );

    setState(() {
      _isSending = true;
      _pending.add(optimistic);
    });
    _messageController.clear();
    _scrollToBottom(animate: true);

    try {
      // Sender derived from JWT — server no longer accepts senderId in body.
      final repo = ref.read(communicationRepositoryProvider);
      await repo.sendMessage(
        conversationId: widget.conversationId,
        content: content,
      );
      // Wait for the fresh list before dropping the optimistic copy (no flicker).
      ref.invalidate(messagesProvider(widget.conversationId));
      await ref.read(messagesProvider(widget.conversationId).future);
      if (!mounted) return;
      setState(() => _pending.removeWhere((m) => m.id == optimistic.id));
      ref.invalidate(conversationsProvider);
      _scrollToBottom(animate: true);
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _pending.removeWhere((m) => m.id == optimistic.id);
        _messageController.text = content;
      });
      final isArabic = Localizations.localeOf(context).languageCode == 'ar';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'تعذر إرسال الرسالة. يرجى المحاولة مرة أخرى.'
            : 'Could not send the message. Please try again.'),
      ));
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    final messagesAsync =
        ref.watch(messagesProvider(widget.conversationId));
    final conversationsAsync = ref.watch(conversationsProvider);
    final myId = ref.watch(authProvider).valueOrNull?.user?.id ?? '';

    // Live refresh + mark-read when a message_received event lands for THIS
    // conversation, then scroll to the new bottom.
    ref.listen(incomingChatMessageProvider, (_, next) {
      // A SignalR stream event can land while this screen is being disposed;
      // touching `ref` after that throws "Cannot use ref after the widget was
      // disposed". Bail if we are no longer mounted.
      if (!mounted) return;
      final convId = next.valueOrNull?['conversationId']?.toString();
      if (convId != null && convId != widget.conversationId) return;
      ref.invalidate(messagesProvider(widget.conversationId));
      _markAsRead();
      _scrollToBottom(animate: true);
    });

    // Counterpart read our messages — refetch so ✓ flips to ✓✓ live.
    ref.listen(chatMessagesReadProvider, (_, next) {
      if (!mounted) return;
      final convId = next.valueOrNull?['conversationId']?.toString();
      if (convId != null && convId != widget.conversationId) return;
      ref.invalidate(messagesProvider(widget.conversationId));
    });
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
                data: (serverMessages) {
                  final messages = [...serverMessages, ..._pending]
                    ..sort((a, b) => a.sentAt.compareTo(b.sentAt));

                  if (messages.isEmpty) {
                    return EmptyState(
                      icon: Icons.chat_bubble_outline,
                      title:
                          isArabic ? 'لا توجد رسائل بعد' : 'No messages yet',
                      subtitle: isArabic
                          ? 'ابدأ المحادثة مع مدربك'
                          : 'Start the conversation with your trainer',
                    );
                  }

                  if (!_didInitialScroll) {
                    _didInitialScroll = true;
                    _scrollToBottom();
                  }

                  return ListView.builder(
                    controller: _scrollController,
                    padding: const EdgeInsets.all(16),
                    itemCount: messages.length,
                    itemBuilder: (context, index) {
                      final msg = messages[index];
                      final isMe = msg.senderId == myId;
                      return _MessageBubble(message: msg, isMe: isMe);
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
                        hintText:
                            isArabic ? 'اكتب رسالتك...' : 'Type a message...',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                        ),
                        contentPadding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 8),
                      ),
                      maxLines: null,
                      textInputAction: TextInputAction.send,
                      onSubmitted: (_) => _sendMessage(),
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton.filled(
                    onPressed: _isSending ? null : _sendMessage,
                    tooltip: isArabic ? 'إرسال' : 'Send',
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
}

/// Timestamp string for a chat bubble. Today's messages show just "h:mm a"
/// so they stay compact; anything older carries an explicit date so the
/// conversation doesn't ambiguously read "04:45" forever (BUG-10).
String _formatSentAt(BuildContext context, DateTime sentAt) {
  final now = DateTime.now();
  final local = sentAt.toLocal();
  final isToday = now.year == local.year &&
      now.month == local.month &&
      now.day == local.day;
  final isArabic = Localizations.localeOf(context).languageCode == 'ar';
  final locale = isArabic ? 'ar' : 'en';
  if (isToday) {
    return DateFormat('h:mm a', locale).format(local);
  }
  return DateFormat('MMM d, h:mm a', locale).format(local);
}

class _MessageBubble extends StatelessWidget {
  final MessageModel message;
  final bool isMe;

  const _MessageBubble({required this.message, required this.isMe});

  bool get _isPending => message.id.startsWith(_optimisticPrefix);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
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
              ? theme.colorScheme.primary
              : theme.colorScheme.surfaceVariant,
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
              style: TextStyle(color: isMe ? Colors.white : null),
            ),
            const SizedBox(height: 2),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  _formatSentAt(context, message.sentAt),
                  style: TextStyle(
                    fontSize: 11,
                    color: isMe
                        ? Colors.white.withOpacity(0.7)
                        : theme.textTheme.bodySmall?.color?.withOpacity(0.7),
                  ),
                ),
                if (isMe) ...[
                  const SizedBox(width: 4),
                  _statusIcon(),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _statusIcon() {
    if (_isPending) {
      return Icon(Icons.access_time,
          size: 12, color: Colors.white.withOpacity(0.7));
    }
    if (message.isRead) {
      return const Icon(Icons.done_all,
          size: 14, color: Colors.lightBlueAccent);
    }
    return Icon(Icons.check, size: 14, color: Colors.white.withOpacity(0.7));
  }
}
