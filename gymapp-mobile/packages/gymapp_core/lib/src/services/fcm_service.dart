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
import 'foreground_notification_presenter.dart';
import 'secure_storage_service.dart';
import 'telemetry_service.dart';

/// Handles Firebase Cloud Messaging for push notifications.
/// Supports silent pushes for config_refresh, gym_suspended, gym_reactivated.
/// No polling -- all real-time updates come through FCM.
class FCMService {
  final FirebaseMessaging? _messaging;
  final SecureStorageService _storage;
  final Ref _ref;
  final ApiClient? _apiClient;
  final ForegroundNotificationPresenter _presenter;
  final TelemetryService? _telemetry;

  /// Set once the listeners are attached so re-login doesn't double-subscribe.
  bool _listening = false;

  /// iOS only: FCM can't issue a token (or join topics) until Apple has
  /// delivered the APNs token, which arrives asynchronously after launch.
  /// Shared so initialize() and the topic subscriptions wait on one check.
  Future<bool>? _apnsWait;
  final Duration _apnsRetryDelay;
  static const int _apnsMaxAttempts = 30;

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
    ForegroundNotificationPresenter? presenter,
    TelemetryService? telemetry,
    Duration apnsRetryDelay = const Duration(milliseconds: 500),
  })  : _ref = ref,
        _telemetry = telemetry,
        _apnsRetryDelay = apnsRetryDelay,
        _storage = storage,
        _apiClient = apiClient,
        _presenter = presenter ?? ForegroundNotificationPresenter(),
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
    if (messaging == null) {
      _reportFailure(_PushStep.firebaseUnavailable);
      return;
    }
    // Request notification permission
    final settings = await messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    final status = settings.authorizationStatus;
    if (status != AuthorizationStatus.authorized &&
        status != AuthorizationStatus.provisional) {
      _reportFailure(_PushStep.permission,
          detail: status);
      return;
    }
    if (!await _apnsReady(messaging)) return;

    await _saveCurrentToken(messaging);
    _setupMessageListeners(messaging);
  }

  Future<void> _saveCurrentToken(FirebaseMessaging messaging) async {
    try {
      final token = await messaging.getToken();
      if (token == null) {
        _reportFailure(_PushStep.nullToken);
        return;
      }
      await _saveToken(token);
    } catch (e) {
      _reportFailure(_PushStep.getToken, error: e);
    }
  }

  Future<void> _saveToken(String token) async {
    await _storage.saveFcmToken(token);
    await _registerTokenWithBackend(token);
  }

  /// Resolves true once FCM can issue tokens. Non-iOS platforms are ready
  /// immediately; on iOS this waits for the APNs token. A failed wait is
  /// not cached, so the next login tries again.
  Future<bool> _apnsReady(FirebaseMessaging messaging) {
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.iOS) {
      return Future.value(true);
    }
    return _apnsWait ??= _waitForApnsToken(messaging).then((ready) {
      if (!ready) _apnsWait = null;
      return ready;
    });
  }

  Future<bool> _waitForApnsToken(FirebaseMessaging messaging) async {
    for (var attempt = 0; attempt < _apnsMaxAttempts; attempt++) {
      if (await messaging.getAPNSToken() != null) return true;
      await Future<void>.delayed(_apnsRetryDelay);
    }
    _reportFailure(_PushStep.apnsToken,
        detail: _apnsRetryDelay * _apnsMaxAttempts);
    return false;
  }

  /// Subscribe to gym-specific topic for targeted notifications.
  Future<void> subscribeToGym(String gymId) =>
      _updateTopic(AppConstants.fcmTopicGym(gymId), subscribe: true);

  /// Unsubscribe from gym topic.
  Future<void> unsubscribeFromGym(String gymId) =>
      _updateTopic(AppConstants.fcmTopicGym(gymId), subscribe: false);

  /// Subscribe to global topic.
  Future<void> subscribeToGlobal() =>
      _updateTopic(AppConstants.fcmTopicGlobal, subscribe: true);

  Future<void> _updateTopic(String topic, {required bool subscribe}) async {
    final messaging = _messaging;
    if (messaging == null || !await _apnsReady(messaging)) return;
    try {
      subscribe
          ? await messaging.subscribeToTopic(topic)
          : await messaging.unsubscribeFromTopic(topic);
    } catch (e) {
      _reportFailure(_PushStep.topic, detail: topic, error: e);
    }
  }

  void _setupMessageListeners(FirebaseMessaging messaging) {
    if (_listening) return;
    _listening = true;

    messaging.onTokenRefresh.listen(_saveToken);

    // iOS: let the OS draw the banner while the app is open.
    messaging.setForegroundNotificationPresentationOptions(
      alert: true,
      badge: true,
      sound: true,
    );
    // Android: FCM won't draw it in the foreground, so re-post it locally.
    _presenter.initialize(
      onTap: (data) => _messageController.add({...data, '_tapped': true}),
    );

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
        // Regular notification -- notification provider refreshes the list;
        // show a banner too since the user is inside the app.
        _presenter.show(message);
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
    } catch (e) {
      // Next refresh or next login will retry. Auth interceptor handles 401.
      _reportFailure(_PushStep.registerToken, error: e);
    }
  }

  /// Push setup fails quietly by design (the app must keep working without
  /// push), so each failed step is reported as a warning — release builds
  /// ship warning+ to the backend client-logs. Never include the token itself.
  void _reportFailure(_PushStep step, {Object? detail, Object? error}) {
    // Step + detail go in the message: the server log line shows only that.
    final text = [
      '[FCMService] push setup failed at ${step.code}: ${step.message}',
      if (detail != null) '$detail',
      if (error != null) '$error',
    ].join(' | ');
    debugPrint(text);
    _telemetry?.log(
      TelemetryLevel.warning,
      text,
      error: error,
      attributes: {
        'push.step': step.code,
        'push.platform': _platformLabel(),
        if (detail != null) 'push.detail': '$detail',
      },
    );
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

/// Push-setup steps that can fail; reported to telemetry by FCMService.
enum _PushStep {
  firebaseUnavailable('firebase_unavailable', 'Firebase not initialized'),
  permission('permission', 'Notification permission not granted'),
  apnsToken('apns_token', 'APNs token never arrived'),
  getToken('get_token', 'getToken failed'),
  nullToken('get_token', 'getToken returned null'),
  registerToken('register_token', 'POST device-tokens failed'),
  topic('topic', 'Topic update failed');

  const _PushStep(this.code, this.message);

  final String code;
  final String message;
}

/// Top-level background message handler (required by Firebase).
@pragma('vm:entry-point')
Future<void> _backgroundMessageHandler(RemoteMessage message) async {
  // Background messages are handled minimally here.
  // The app processes them fully when it returns to foreground.
}
