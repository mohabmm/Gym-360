import 'dart:convert';

import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart'
    show debugPrint, defaultTargetPlatform, TargetPlatform;
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

/// Shows a heads-up banner for pushes that arrive while the app is open.
///
/// FCM never displays `notification` payloads on Android while the app is in
/// the foreground, so we re-post them through a local notification. On iOS
/// the native presentation options (set in [FCMService]) already show the
/// banner, so this presenter is a no-op there to avoid duplicates.
///
/// Background / terminated pushes are drawn by the OS itself and land in the
/// same [channelId] channel via the AndroidManifest default-channel meta-data.
class ForegroundNotificationPresenter {
  /// Must match `default_notification_channel_id` in each AndroidManifest and
  /// `channel_id` in the backend's FCM payload.
  static const String channelId = 'gym360_default';

  static const AndroidNotificationChannel _channel = AndroidNotificationChannel(
    channelId,
    'Gym 360 / جيم 360',
    description: 'Sessions, messages, memberships and gym announcements',
    importance: Importance.high,
  );

  final FlutterLocalNotificationsPlugin _plugin;
  bool _ready = false;

  ForegroundNotificationPresenter({FlutterLocalNotificationsPlugin? plugin})
      : _plugin = plugin ?? FlutterLocalNotificationsPlugin();

  bool get _isAndroid => defaultTargetPlatform == TargetPlatform.android;

  /// Creates the Android channel and wires taps on the banner back to
  /// [onTap] with the original FCM data map.
  Future<void> initialize({
    required void Function(Map<String, dynamic> data) onTap,
  }) async {
    if (!_isAndroid || _ready) return;
    try {
      await _plugin.initialize(
        settings: const InitializationSettings(
          android: AndroidInitializationSettings('@mipmap/ic_launcher'),
        ),
        onDidReceiveNotificationResponse: (response) =>
            onTap(decodePayload(response.payload)),
      );
      await _plugin
          .resolvePlatformSpecificImplementation<
              AndroidFlutterLocalNotificationsPlugin>()
          ?.createNotificationChannel(_channel);
      _ready = true;
    } catch (e) {
      debugPrint('[ForegroundNotificationPresenter] init failed: $e');
    }
  }

  /// Displays [message] if it carries a visible `notification` block.
  /// Data-only (silent) pushes such as config_refresh are ignored.
  Future<void> show(RemoteMessage message) async {
    final notification = message.notification;
    if (!_ready || notification == null) return;
    await _plugin.show(
      id: notification.hashCode,
      title: notification.title,
      body: notification.body,
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          _channel.id,
          _channel.name,
          channelDescription: _channel.description,
          importance: Importance.high,
          priority: Priority.high,
        ),
      ),
      payload: jsonEncode(message.data),
    );
  }

  /// Decodes the JSON payload stored on a local notification back to the
  /// FCM data map. Malformed or missing payloads decode to an empty map.
  static Map<String, dynamic> decodePayload(String? payload) {
    if (payload == null || payload.isEmpty) return const {};
    try {
      final decoded = jsonDecode(payload);
      return decoded is Map<String, dynamic> ? decoded : const {};
    } on FormatException {
      return const {};
    }
  }
}
