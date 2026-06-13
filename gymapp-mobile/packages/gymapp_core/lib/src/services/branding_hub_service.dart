import 'package:logger/logger.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../network/api_base_url.dart';

typedef BrandingUpdatedHandler = void Function(Map<String, dynamic> payload);

/// SignalR client for the config hub. Listens for `branding_updated` events
/// scoped to a single gym, so connected mobile clients can drop their cached
/// gym branding and refetch without re-login.
///
/// Lifecycle:
///   final svc = BrandingHubService(accessToken: jwt);
///   await svc.connect();
///   await svc.subscribeToGym(gymId);
///   svc.onBrandingUpdated = (payload) => ref.invalidate(gymBrandingProvider);
///   ...
///   await svc.disconnect();  // on logout
class BrandingHubService {
  BrandingHubService({required this.accessToken});

  final String accessToken;
  final Logger _log = Logger();

  HubConnection? _hub;
  String? _subscribedGymId;

  BrandingUpdatedHandler? onBrandingUpdated;

  bool get isConnected =>
      _hub != null && _hub!.state == HubConnectionState.Connected;

  String get _hubUrl {
    final base = ApiBaseUrl.resolve();
    final root = base.replaceAll(RegExp(r'/api/v\d+/?$'), '');
    return '$root/hubs/config';
  }

  Future<void> connect() async {
    if (isConnected) return;

    final hub = HubConnectionBuilder()
        .withUrl(
          _hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => accessToken,
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000, 30000])
        .build();

    hub.on('branding_updated', (args) {
      final data = _firstArg(args);
      if (data != null) onBrandingUpdated?.call(data);
    });

    // Re-subscribe after a reconnect so the new connection picks up gym events.
    hub.onreconnected(({connectionId}) async {
      final gymId = _subscribedGymId;
      if (gymId != null) {
        try {
          await hub.invoke('SubscribeToGym', args: [gymId]);
        } catch (e) {
          _log.w('BrandingHub: re-subscribe failed: $e');
        }
      }
    });

    try {
      await hub.start();
      _hub = hub;
    } catch (e, st) {
      _log.w('BrandingHub: hub.start failed', error: e, stackTrace: st);
      _hub = null;
    }
  }

  Future<void> subscribeToGym(String gymId) async {
    final hub = _hub;
    if (hub == null || hub.state != HubConnectionState.Connected) return;
    try {
      await hub.invoke('SubscribeToGym', args: [gymId]);
      _subscribedGymId = gymId;
    } catch (e) {
      _log.w('SubscribeToGym failed: $e');
    }
  }

  Future<void> unsubscribeFromGym(String gymId) async {
    final hub = _hub;
    if (hub == null || hub.state != HubConnectionState.Connected) return;
    try {
      await hub.invoke('UnsubscribeFromGym', args: [gymId]);
    } catch (e) {
      _log.w('UnsubscribeFromGym failed: $e');
    }
    if (_subscribedGymId == gymId) _subscribedGymId = null;
  }

  Future<void> disconnect() async {
    try {
      await _hub?.stop();
    } catch (_) {}
    _hub = null;
    _subscribedGymId = null;
  }

  Map<String, dynamic>? _firstArg(List<Object?>? args) {
    if (args == null || args.isEmpty) return null;
    final first = args.first;
    if (first is Map) return Map<String, dynamic>.from(first);
    return null;
  }
}
