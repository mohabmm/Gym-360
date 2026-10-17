import 'package:dio/dio.dart';
import 'package:logger/logger.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../constants/api_endpoints.dart';
import '../models/message_model.dart';
import '../network/api_base_url.dart';
import '../network/api_client.dart';
import '../types/entity_ids.dart';
import 'chat_requests.dart';

export 'chat_requests.dart';

typedef MessageReceivedHandler = void Function(Map<String, dynamic> payload);
typedef MessagesReadHandler = void Function(Map<String, dynamic> payload);
typedef ConversationCreatedHandler = void Function(Map<String, dynamic> payload);

/// Cross-app chat client. REST methods for list/load/send/read; SignalR hub for
/// real-time message_received / messages_read / conversation_created events.
///
/// Usage:
///   final svc = ChatService(api, accessToken: token);
///   await svc.connectHub();
///   svc.onMessageReceived = (msg) { ... };
///   await svc.joinConversation(conversationId);
class ChatService {
  ChatService(this._api, {required this.accessToken});

  final ApiClient _api;
  final String accessToken;
  final Logger _log = Logger();

  HubConnection? _hub;

  MessageReceivedHandler? onMessageReceived;
  MessagesReadHandler? onMessagesRead;
  ConversationCreatedHandler? onConversationCreated;

  // ---------- REST ----------

  Future<List<ConversationModel>> listConversations({int page = 1, int pageSize = 50}) async {
    final res = await _api.dio.get(
      ApiEndpoints.conversations,
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return _pageItems(res, ConversationModel.fromJson);
  }

  Future<List<MessageModel>> listMessages(String conversationId, {int page = 1, int pageSize = 100}) async {
    final res = await _api.dio.get(
      ApiEndpoints.messages(ConversationId(conversationId)),
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return _pageItems(res, MessageModel.fromJson);
  }

  /// Unwraps `{ data: { items: [...] } }` into typed models.
  List<T> _pageItems<T>(
    Response<dynamic> res,
    T Function(Map<String, dynamic> json) fromJson,
  ) {
    final items = (res.data?['data']?['items'] as List?) ?? const [];
    return items
        .whereType<Map>()
        .map((m) => fromJson(Map<String, dynamic>.from(m)))
        .toList();
  }

  Future<String> createOrGetConversation(ConversationParticipants participants) async {
    final res = await _api.dio.post(
      ApiEndpoints.conversations,
      data: participants.toJson(),
    );
    final id = res.data?['data'];
    if (id is String) return id;
    throw DioException(
      requestOptions: res.requestOptions,
      error: 'Unexpected response shape from createOrGetConversation',
    );
  }

  Future<String> sendMessage(String conversationId, MessageDraft draft) async {
    final res = await _api.dio.post(
      ApiEndpoints.messages(ConversationId(conversationId)),
      data: draft.toJson(),
    );
    return res.data?['data'] as String;
  }

  Future<void> markRead(String conversationId) async {
    await _api.dio.put('${ApiEndpoints.conversations}/$conversationId/read');
  }

  // ---------- SignalR ----------

  String get _hubUrl {
    final base = ApiBaseUrl.resolve();
    final root = base.replaceAll(RegExp(r'/api/v\d+/?$'), '');
    return '$root/hubs/chat';
  }

  Future<void> connectHub() async {
    if (_isConnected(_hub)) return;

    final hub = HubConnectionBuilder()
        .withUrl(
          _hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => accessToken,
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000, 30000])
        .build();

    _bindHandlers(hub);

    try {
      await hub.start();
      _hub = hub;
    } catch (e, st) {
      _log.w('ChatService: hub.start failed', error: e, stackTrace: st);
      _hub = null;
    }
  }

  Future<void> joinConversation(String conversationId) async {
    final hub = _connectedHub;
    if (hub == null) return;
    try {
      await hub.invoke('JoinConversation', args: [conversationId]);
    } catch (e) {
      _log.w('JoinConversation failed: $e');
    }
  }

  Future<void> leaveConversation(String conversationId) async {
    final hub = _connectedHub;
    if (hub == null) return;
    try {
      await hub.invoke('LeaveConversation', args: [conversationId]);
    } catch (e) {
      _log.w('LeaveConversation failed: $e');
    }
  }

  Future<void> disconnect() async {
    try {
      await _hub?.stop();
    } catch (_) {}
    _hub = null;
  }

  HubConnection? get _connectedHub => _isConnected(_hub) ? _hub : null;

  static bool _isConnected(HubConnection? hub) =>
      hub != null && hub.state == HubConnectionState.Connected;

  void _bindHandlers(HubConnection hub) {
    hub.on('message_received', _forward(() => onMessageReceived));
    hub.on('messages_read', _forward(() => onMessagesRead));
    hub.on('conversation_created', _forward(() => onConversationCreated));
  }

  /// Forwards the event's first map argument to the handler current at
  /// dispatch time (handlers may be (re)assigned after [connectHub]).
  MethodInvocationFunc _forward(
    void Function(Map<String, dynamic> payload)? Function() handler,
  ) {
    return (args) {
      final data = _firstArg(args);
      if (data != null) handler()?.call(data);
    };
  }

  Map<String, dynamic>? _firstArg(List<Object?>? args) {
    if (args == null || args.isEmpty) return null;
    final first = args.first;
    if (first is Map) return Map<String, dynamic>.from(first);
    return null;
  }
}
