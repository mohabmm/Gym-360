import 'dart:async';

import '../network/api_client.dart';
import 'chat_service.dart';

/// Session-scoped wrapper around [ChatService]'s SignalR hub that re-exposes the
/// hub callbacks as broadcast streams (so multiple chat screens can listen) and
/// survives the connect race (join requests issued before the hub finishes
/// connecting are replayed once connected).
///
/// Mobile realtime for chat rides SignalR — the backend fans messages out only
/// through `IChatNotifier` → `/hubs/chat` (no FCM push on message send), and the
/// app already uses SignalR on mobile for branding. FCM stays for the separate
/// notification path (deploy/config/announcement pushes), not chat.
class ChatRealtime {
  ChatService? _svc;
  String? _pendingJoin;
  bool _connected = false;
  bool _disposed = false;

  final _received = StreamController<Map<String, dynamic>>.broadcast();
  final _read = StreamController<Map<String, dynamic>>.broadcast();
  final _created = StreamController<Map<String, dynamic>>.broadcast();

  /// Emits the payload of every `message_received` hub event.
  Stream<Map<String, dynamic>> get onMessageReceived => _received.stream;

  /// Emits the payload of every `messages_read` hub event — lets a sender flip
  /// their own ✓ → ✓✓ ticks live when the counterpart reads.
  Stream<Map<String, dynamic>> get onMessagesRead => _read.stream;

  /// Emits the payload of every `conversation_created` hub event.
  Stream<Map<String, dynamic>> get onConversationCreated => _created.stream;

  /// Connect the hub once for this instance. Safe to call repeatedly — only the
  /// first call wires up and connects.
  Future<void> connect(ApiClient api, String accessToken) async {
    if (_disposed || _svc != null) return;
    final svc = ChatService(api, accessToken: accessToken);
    svc.onMessageReceived = (d) {
      if (!_received.isClosed) _received.add(d);
    };
    svc.onMessagesRead = (d) {
      if (!_read.isClosed) _read.add(d);
    };
    svc.onConversationCreated = (d) {
      if (!_created.isClosed) _created.add(d);
    };
    _svc = svc;

    await svc.connectHub();
    if (_disposed) {
      await svc.disconnect();
      return;
    }
    _connected = true;
    // Replay a join requested before the connection was ready.
    final pending = _pendingJoin;
    if (pending != null) await svc.joinConversation(pending);
  }

  /// Subscribe to a conversation group so `messages_read` (and the in-thread
  /// copy of `message_received`) arrive. The per-user group auto-join on the
  /// server already covers inbox delivery, so this is best-effort.
  Future<void> joinConversation(String conversationId) async {
    _pendingJoin = conversationId;
    if (_connected) await _svc?.joinConversation(conversationId);
  }

  Future<void> leaveConversation(String conversationId) async {
    if (_pendingJoin == conversationId) _pendingJoin = null;
    if (_connected) await _svc?.leaveConversation(conversationId);
  }

  void dispose() {
    _disposed = true;
    _svc?.disconnect();
    _received.close();
    _read.close();
    _created.close();
  }
}
