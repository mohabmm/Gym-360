import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/communication_providers.dart';

/// Local prefix that marks an optimistic (not-yet-confirmed) message so the
/// bubble can render a "sending" clock instead of a delivery tick.
const _optimisticPrefix = 'optimistic-';

class ChatScreen extends ConsumerStatefulWidget {
  final String conversationId;

  const ChatScreen({super.key, required this.conversationId});

  @override
  ConsumerState<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends ConsumerState<ChatScreen> {
  final _messageController = TextEditingController();
  final _scrollController = ScrollController();
  bool _isSending = false;

  /// Cached realtime handle captured in [initState] so [dispose] can leave the
  /// SignalR group without touching `ref` — reading a provider after the
  /// element is disposed throws "Cannot use ref after the widget was disposed".
  ChatRealtime? _realtime;

  /// Optimistic messages shown immediately on send. Removed once the refetched
  /// server list contains the confirmed copy (or on failure rollback).
  final List<MessageModel> _pending = [];

  /// Ensures we only auto-scroll to the bottom on the first data render.
  bool _didInitialScroll = false;

  @override
  void initState() {
    super.initState();
    // Capture the realtime handle now; `ref` is safe in initState but not in
    // dispose, where we still need to leave the conversation group.
    _realtime = ref.read(chatRealtimeProvider);
    // Clear the unread badge for this conversation as soon as it opens, so the
    // trainer's home "unread" counter and the conversation list stay accurate
    // (parity with the trainee app).
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _markAsRead();
      // Join the SignalR conversation group so messages_read / in-thread
      // message_received events arrive live.
      _realtime?.joinConversation(widget.conversationId);
    });
  }

  @override
  void dispose() {
    _realtime?.leaveConversation(widget.conversationId);
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _markAsRead() async {
    try {
      final apiClient = ref.read(apiClientProvider);
      // Caller derived from JWT — no body.
      await apiClient
          .putVoid('${ApiEndpoints.conversationById(widget.conversationId)}/read');
      ref.invalidate(conversationsProvider);
    } catch (_) {
      // Marking read is non-critical — fail silently.
    }
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

    final myId = ref.read(authProvider).user?.id ?? '';
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
      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.messages(widget.conversationId),
        data: {'content': content},
      );
      // Wait for the fresh list so we only drop the optimistic copy once the
      // confirmed message is present — avoids a flicker.
      ref.invalidate(conversationMessagesProvider(widget.conversationId));
      await ref
          .read(conversationMessagesProvider(widget.conversationId).future);
      if (!mounted) return;
      setState(() => _pending.removeWhere((m) => m.id == optimistic.id));
      ref.invalidate(conversationsProvider);
      _scrollToBottom(animate: true);
    } catch (e) {
      if (!mounted) return;
      // Rollback the optimistic bubble and restore the text for a retry.
      setState(() {
        _pending.removeWhere((m) => m.id == optimistic.id);
        _messageController.text = content;
      });
      final isArabic = Localizations.localeOf(context).languageCode == 'ar';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'تعذر إرسال الرسالة. حاول مرة أخرى.'
            : 'Could not send the message. Try again.'),
      ));
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final messagesAsync =
        ref.watch(conversationMessagesProvider(widget.conversationId));
    final auth = ref.watch(authProvider);
    final theme = Theme.of(context);

    final title = ref.watch(conversationTitleProvider(widget.conversationId));

    // Live refresh + mark-read when a message_received event lands for THIS
    // conversation, then scroll to the new bottom.
    ref.listen(incomingChatMessageProvider, (_, next) {
      // A SignalR stream event can land while this screen is being disposed;
      // touching `ref` after that throws "Cannot use ref after the widget was
      // disposed". Bail if we are no longer mounted.
      if (!mounted) return;
      final convId = next.valueOrNull?['conversationId']?.toString();
      if (convId != null && convId != widget.conversationId) return;
      ref.invalidate(conversationMessagesProvider(widget.conversationId));
      _markAsRead();
      _scrollToBottom(animate: true);
    });

    // Counterpart read our messages — refetch so ✓ flips to ✓✓ live.
    ref.listen(chatMessagesReadProvider, (_, next) {
      if (!mounted) return;
      final convId = next.valueOrNull?['conversationId']?.toString();
      if (convId != null && convId != widget.conversationId) return;
      ref.invalidate(conversationMessagesProvider(widget.conversationId));
    });

    return Scaffold(
      appBar: AppBar(
        title: Text(title ?? (isArabic ? 'محادثة' : 'Chat')),
      ),
      body: Column(
        children: [
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(
                  conversationMessagesProvider(widget.conversationId)),
              child: messagesAsync.when(
                data: (serverMessages) {
                  // Merge confirmed + optimistic, keep chronological order.
                  final messages = [...serverMessages, ..._pending]
                    ..sort((a, b) => a.sentAt.compareTo(b.sentAt));

                  if (messages.isEmpty) {
                    return EmptyState(
                      icon: Icons.chat_bubble_outline,
                      title:
                          isArabic ? 'ابدا المحادثة' : 'Start the conversation',
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
                      final isMe = msg.senderId == auth.user?.id;
                      return _MessageBubble(
                        message: msg,
                        isMe: isMe,
                        isArabic: isArabic,
                      );
                    },
                  );
                },
                loading: () =>
                    const Center(child: CircularProgressIndicator()),
                error: (e, _) => AppErrorWidget(
                  message: e.toString(),
                  onRetry: () => ref.invalidate(
                      conversationMessagesProvider(widget.conversationId)),
                ),
              ),
            ),
          ),

          // Input bar
          Container(
            padding: EdgeInsets.fromLTRB(
                12, 8, 12, 12 + MediaQuery.of(context).padding.bottom),
            decoration: BoxDecoration(
              color: theme.scaffoldBackgroundColor,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.05),
                  blurRadius: 4,
                  offset: const Offset(0, -2),
                ),
              ],
            ),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _messageController,
                    decoration: InputDecoration(
                      hintText:
                          isArabic ? 'اكتب رسالة...' : 'Type a message...',
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
                  icon: _isSending
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : const Icon(Icons.send),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _MessageBubble extends StatelessWidget {
  final MessageModel message;
  final bool isMe;
  final bool isArabic;

  const _MessageBubble({
    required this.message,
    required this.isMe,
    required this.isArabic,
  });

  bool get _isPending => message.id.startsWith(_optimisticPrefix);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Align(
      alignment: isMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        decoration: BoxDecoration(
          color: isMe ? theme.colorScheme.primary : theme.colorScheme.surface,
          borderRadius: BorderRadius.only(
            topLeft: const Radius.circular(12),
            topRight: const Radius.circular(12),
            bottomLeft: Radius.circular(isMe ? 12 : 0),
            bottomRight: Radius.circular(isMe ? 0 : 12),
          ),
          boxShadow: [
            BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 4),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              message.content,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: isMe ? Colors.white : theme.colorScheme.onSurface,
              ),
            ),
            const SizedBox(height: 2),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  DateFormat('hh:mm a', isArabic ? 'ar' : 'en')
                      .format(message.sentAt.toLocal()),
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: isMe
                        ? Colors.white.withOpacity(0.7)
                        : AppColors.grey400,
                    fontSize: 10,
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
      // Delivered + read.
      return const Icon(Icons.done_all, size: 14, color: Colors.lightBlueAccent);
    }
    // Sent (server-confirmed) but not yet read.
    return Icon(Icons.check, size: 14, color: Colors.white.withOpacity(0.7));
  }
}
