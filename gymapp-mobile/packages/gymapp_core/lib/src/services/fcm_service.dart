import 'dart:async';
import 'dart:io' show Platform;
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart' show debugPrint, defaultTargetPlatform, TargetPlatform, kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../constants/api_endpoints.dart';
import '../constants/app_constants.dart';
import '../models/notification_model.dart';
import '../network/api_client.dart';
import '../widgets/feature_gate.dart';
import 'secure_storage_service.dart';

/// Handles Firebase Cloud Messaging for push notifications.
/// Supports silent pushes for config_refresh, gym_suspended, gym_reactivated.
/// No polling -- all real-time updates come through FCM.
class FCMService {
  final FirebaseMessaging? _messaging;
  final SecureStorageService _storage;
  final Ref _ref;
  final ApiClient? _apiClient;

  /// True when a default Firebase app is initialized and FCM is usable.
  /// When false, all FCM operations become safe no-ops so the app still runs
  /// in dev environments without google-services.json / GoogleService-Info.plist.
  bool get isAvailable => _messaging != null;

  final StreamController<Map<String, dynamic>> _messageController =
      StreamController<Map<String, dynamic>>.broadcast();

  final StreamController<bool> _gymSuspensionController =
      StreamController<bool>.broadcast();

  final StreamController<void> _configRefreshController =
      StreamController<void>.broadcast();

  /// Stream of all incoming data messages.
  Stream<Map<String, dynamic>> get onMessage => _messageController.stream;

  /// Stream of gym suspension state changes.
  /// true = suspended, false = reactivated.
  Stream<bool> get onGymSuspensionChanged => _gymSuspensionController.stream;

  /// Stream that emits when config should be refreshed.
  Stream<void> get onConfigRefresh => _configRefreshController.stream;

  FCMService({
    required Ref ref,
    required SecureStorageService storage,
    ApiClient? apiClient,
    FirebaseMessaging? messaging,
  })  : _ref = ref,
        _storage = storage,
        _apiClient = apiClient,
        _messaging = messaging ?? _resolveMessaging();

  static FirebaseMessaging? _resolveMessaging() {
    // FirebaseMessaging.instance throws if no default Firebase app exists
    // (e.g. dev builds without google-services.json). Treat that as "FCM off".
    if (Firebase.apps.isEmpty) {
      debugPrint('[FCMService] Firebase not initialized — FCM disabled.');
      return null;
    }
    try {
      return FirebaseMessaging.instance;
    } catch (e) {
      debugPrint('[FCMService] FirebaseMessaging unavailable: $e');
      return null;
    }
  }

  /// Initialize FCM: request permissions, get token, set up listeners.
  Future<void> initialize() async {
    final messaging = _messaging;
    if (messaging == null) return;
    // Request notification permission
    final settings = await messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    if (settings.authorizationStatus == AuthorizationStatus.authorized ||
        settings.authorizationStatus == AuthorizationStatus.provisional) {
      // Get and save FCM token
      final token = await messaging.getToken();
      if (token != null) {
        await _storage.saveFcmToken(token);
        await _registerTokenWithBackend(token);
      }

      // Listen for token refreshes
      messaging.onTokenRefresh.listen((newToken) async {
        await _storage.saveFcmToken(newToken);
        await _registerTokenWithBackend(newToken);
      });

      // Set up message handlers
      _setupMessageListeners(messaging);
    }
  }

  /// Subscribe to gym-specific topic for targeted notifications.
  Future<void> subscribeToGym(String gymId) async {
    await _messaging?.subscribeToTopic(AppConstants.fcmTopicGym(gymId));
  }

  /// Unsubscribe from gym topic.
  Future<void> unsubscribeFromGym(String gymId) async {
    await _messaging?.unsubscribeFromTopic(AppConstants.fcmTopicGym(gymId));
  }

  /// Subscribe to global topic.
  Future<void> subscribeToGlobal() async {
    await _messaging?.subscribeToTopic(AppConstants.fcmTopicGlobal);
  }

  void _setupMessageListeners(FirebaseMessaging messaging) {
    // Foreground messages
    FirebaseMessaging.onMessage.listen(_handleMessage);

    // Background/terminated messages
    FirebaseMessaging.onBackgroundMessage(_backgroundMessageHandler);

    // When user taps notification and app opens
    FirebaseMessaging.onMessageOpenedApp.listen(_handleMessageTap);

    // Check if app was opened from a notification
    messaging.getInitialMessage().then((message) {
      if (message != null) {
        _handleMessageTap(message);
      }
    });
  }

  void _handleMessage(RemoteMessage message) {
    final data = message.data;
    _messageController.add(data);

    final type = data['type'] as String?;

    switch (type) {
      case NotificationType.configRefresh:
        // Invalidate planFeaturesProvider so every FeatureGate rebuilds
        _ref.invalidate(planFeaturesProvider);
        _configRefreshController.add(null);
        break;
      case NotificationType.gymSuspended:
        _gymSuspensionController.add(true);
        break;
      case NotificationType.gymReactivated:
        _gymSuspensionController.add(false);
        break;
      default:
        // Regular notification -- handled by notification provider
        break;
    }
  }

  void _handleMessageTap(RemoteMessage message) {
    final data = message.data;
    _messageController.add({...data, '_tapped': true});
  }

  void dispose() {
    _messageController.close();
    _gymSuspensionController.close();
    _configRefreshController.close();
  }

  Future<void> _registerTokenWithBackend(String token) async {
    final apiClient = _apiClient;
    if (apiClient == null) return;
    try {
      await apiClient.dio.post(
        ApiEndpoints.deviceTokens,
        data: {'token': token, 'platform': _platformLabel()},
      );
    } catch (_) {
      // Swallow — next refresh or next login will retry. Auth interceptor handles 401.
    }
  }

  static String _platformLabel() {
    if (kIsWeb) return 'Web';
    switch (defaultTargetPlatform) {
      case TargetPlatform.iOS:
        return 'iOS';
      case TargetPlatform.android:
        return 'Android';
      default:
        return Platform.operatingSystem;
    }
  }
}

/// Top-level background message handler (required by Firebase).
@pragma('vm:entry-point')
Future<void> _backgroundMessageHandler(RemoteMessage message) async {
  // Background messages are handled minimally here.
  // The app processes them fully when it returns to foreground.
}
