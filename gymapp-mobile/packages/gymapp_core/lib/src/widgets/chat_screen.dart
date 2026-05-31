import 'package:flutter/material.dart';

import '../models/message_model.dart';
import '../network/api_client.dart';
import '../services/chat_service.dart';

/// Shared chat screen used by trainer + trainee apps. Real-time updates via
/// SignalR when reachable; falls back to refresh-on-send REST polling otherwise.
class ChatScreen extends StatefulWidget {
  const ChatScreen({
    super.key,
    required this.apiClient,
    required this.accessToken,
    required this.currentUserId,
    this.title = 'Messages',
  });

  final ApiClient apiClient;
  final String accessToken;
  final String currentUserId;
  final String title;

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  late final ChatService _svc;
  List<ConversationModel> _conversations = const [];
  ConversationModel? _selected;
  List<MessageModel> _messages = const [];
  final TextEditingController _draft = TextEditingController();
  final ScrollController _scroll = ScrollController();
  bool _loadingConvs = true;
  bool _loadingMsgs = false;
  bool _sending = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _svc = ChatService(widget.apiClient, accessToken: widget.accessToken)
      ..onMessageReceived = _onMessageReceived
      ..onMessagesRead = _onMessagesRead
      ..onConversationCreated = _onConvCreated;
    _initialize();
  }

  Future<void> _initialize() async {
    try {
      await _svc.connectHub();
    } catch (_) {/* fallback to REST-only */}
    await _loadConversations();
  }

  Future<void> _loadConversations() async {
    setState(() {
      _loadingConvs = true;
      _error = null;
    });
    try {
      final list = await _svc.listConversations();
      setState(() {
        _conversations = list;
        _loadingConvs = false;
        if (_selected == null && list.isNotEmpty) {
          _selectConversation(list.first);
        }
      });
    } catch (e) {
      setState(() {
        _error = 'Failed to load conversations';
        _loadingConvs = false;
      });
    }
  }

  Future<void> _selectConversation(ConversationModel c) async {
    if (_selected != null) {
      unawaited(_svc.leaveConversation(_selected!.id));
    }
    setState(() {
      _selected = c;
      _messages = const [];
      _loadingMsgs = true;
    });
    try {
      await _svc.joinConversation(c.id);
    } catch (_) {}
    try {
      final msgs = await _svc.listMessages(c.id);
      msgs.sort((a, b) => a.sentAt.compareTo(b.sentAt));
      setState(() {
        _messages = msgs;
        _loadingMsgs = false;
      });
      _scrollBottom();
      unawaited(_svc.markRead(c.id).then((_) => _loadConversations()));
    } catch (e) {
      setState(() => _loadingMsgs = false);
    }
  }

  void _onMessageReceived(Map<String, dynamic> payload) {
    final convId = payload['conversationId'] as String?;
    if (convId == null) return;
    if (_selected?.id == convId) {
      // Append optimistically by re-fetching last batch
      _svc.listMessages(convId).then((msgs) {
        msgs.sort((a, b) => a.sentAt.compareTo(b.sentAt));
        if (mounted) {
          setState(() => _messages = msgs);
          _scrollBottom();
        }
      }).catchError((_) {});
    }
    _loadConversations();
  }

  void _onMessagesRead(Map<String, dynamic> _) => _loadConversations();
  void _onConvCreated(Map<String, dynamic> _) => _loadConversations();

  Future<void> _send() async {
    final text = _draft.text.trim();
    final selected = _selected;
    if (text.isEmpty || selected == null || _sending) return;
    setState(() => _sending = true);
    final originalText = text;
    _draft.clear();
    try {
      await _svc.sendMessage(selected.id, originalText);
      // Refresh thread (hub will also push)
      final msgs = await _svc.listMessages(selected.id);
      msgs.sort((a, b) => a.sentAt.compareTo(b.sentAt));
      if (mounted) setState(() => _messages = msgs);
      _scrollBottom();
    } catch (_) {
      _draft.text = originalText;
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  void _scrollBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scroll.hasClients) {
        _scroll.animateTo(
          _scroll.position.maxScrollExtent,
          duration: const Duration(milliseconds: 200),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  void dispose() {
    _draft.dispose();
    _scroll.dispose();
    _svc.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(widget.title)),
      body: _selected == null
          ? _buildList(context, theme)
          : Column(
              children: [
                _buildSelectedHeader(theme),
                Expanded(child: _buildThread(theme)),
                _buildComposer(theme),
              ],
            ),
    );
  }

  Widget _buildList(BuildContext context, ThemeData theme) {
    if (_loadingConvs) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(child: Text(_error!, style: const TextStyle(color: Colors.red)));
    }
    if (_conversations.isEmpty) {
      return const Center(child: Text('No conversations yet'));
    }
    return ListView.separated(
      itemCount: _conversations.length,
      separatorBuilder: (_, __) => const Divider(height: 1),
      itemBuilder: (_, i) {
        final c = _conversations[i];
        final other = c.participants.firstWhere(
          (p) => p.userId != widget.currentUserId,
          orElse: () => c.participants.first,
        );
        final last = c.lastMessage?.content ?? '(no messages)';
        return ListTile(
          leading: CircleAvatar(child: Text(other.name.isNotEmpty ? other.name[0].toUpperCase() : '?')),
          title: Text(other.name),
          subtitle: Text(last, maxLines: 1, overflow: TextOverflow.ellipsis),
          trailing: c.unreadCount > 0
              ? CircleAvatar(
                  radius: 12,
                  backgroundColor: theme.colorScheme.primary,
                  child: Text(
                    '${c.unreadCount}',
                    style: TextStyle(color: theme.colorScheme.onPrimary, fontSize: 11),
                  ),
                )
              : null,
          onTap: () => _selectConversation(c),
        );
      },
    );
  }

  Widget _buildSelectedHeader(ThemeData theme) {
    final selected = _selected;
    if (selected == null) return const SizedBox.shrink();
    final other = selected.participants.firstWhere(
      (p) => p.userId != widget.currentUserId,
      orElse: () => selected.participants.first,
    );
    return Material(
      color: theme.colorScheme.surfaceVariant,
      child: ListTile(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => setState(() => _selected = null),
        ),
        title: Text(other.name),
        subtitle: Text(other.role),
      ),
    );
  }

  Widget _buildThread(ThemeData theme) {
    if (_loadingMsgs) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_messages.isEmpty) {
      return const Center(child: Text('Say hello'));
    }
    return ListView.builder(
      controller: _scroll,
      padding: const EdgeInsets.all(12),
      itemCount: _messages.length,
      itemBuilder: (_, i) {
        final m = _messages[i];
        final mine = m.senderId == widget.currentUserId;
        return Align(
          alignment: mine ? Alignment.centerRight : Alignment.centerLeft,
          child: Container(
            margin: const EdgeInsets.symmetric(vertical: 4),
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: mine ? theme.colorScheme.primary : theme.colorScheme.surfaceVariant,
              borderRadius: BorderRadius.only(
                topLeft: const Radius.circular(16),
                topRight: const Radius.circular(16),
                bottomLeft: Radius.circular(mine ? 16 : 4),
                bottomRight: Radius.circular(mine ? 4 : 16),
              ),
            ),
            child: Column(
              crossAxisAlignment: mine ? CrossAxisAlignment.end : CrossAxisAlignment.start,
              children: [
                if (!mine)
                  Text(
                    m.senderName ?? 'Unknown',
                    style: theme.textTheme.labelSmall?.copyWith(fontWeight: FontWeight.w600),
                  ),
                Text(
                  m.content,
                  style: TextStyle(
                    color: mine ? theme.colorScheme.onPrimary : theme.colorScheme.onSurface,
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildComposer(ThemeData theme) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 12),
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: _draft,
                minLines: 1,
                maxLines: 4,
                decoration: const InputDecoration(
                  hintText: 'Type a message...',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
              ),
            ),
            const SizedBox(width: 8),
            FilledButton.icon(
              onPressed: _sending ? null : _send,
              icon: const Icon(Icons.send, size: 18),
              label: Text(_sending ? '...' : 'Send'),
            ),
          ],
        ),
      ),
    );
  }
}

void unawaited(Future<dynamic> _) {}
