import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// APNs token shows up only after [apnsAfter] polls (null = never).
class _FakeMessaging implements FirebaseMessaging {
  _FakeMessaging({this.apnsAfter, this.permission = _authorized});

  final int? apnsAfter;
  final NotificationSettings permission;
  int apnsPolls = 0;
  final List<String> subscribed = [];
  bool tokenRequested = false;

  @override
  Future<String?> getAPNSToken() async {
    apnsPolls++;
    final after = apnsAfter;
    return after != null && apnsPolls > after ? 'apns' : null;
  }

  @override
  Future<void> subscribeToTopic(String topic) async => subscribed.add(topic);

  @override
  Future<String?> getToken({
    String? serviceWorkerScriptPath,
    String? vapidKey,
  }) async {
    tokenRequested = true;
    return 'fcm-token';
  }

  /// requestPermission is routed here so the fake needn't mirror its
  /// eight-parameter signature.
  @override
  dynamic noSuchMethod(Invocation invocation) {
    if (invocation.memberName == #requestPermission) {
      return Future.value(permission);
    }
    throw UnimplementedError('${invocation.memberName}');
  }
}

const _authorized = NotificationSettings(
  alert: AppleNotificationSetting.enabled,
  announcement: AppleNotificationSetting.disabled,
  authorizationStatus: AuthorizationStatus.authorized,
  badge: AppleNotificationSetting.enabled,
  carPlay: AppleNotificationSetting.disabled,
  lockScreen: AppleNotificationSetting.enabled,
  notificationCenter: AppleNotificationSetting.enabled,
  showPreviews: AppleShowPreviewSetting.always,
  timeSensitive: AppleNotificationSetting.disabled,
  criticalAlert: AppleNotificationSetting.disabled,
  sound: AppleNotificationSetting.enabled,
  providesAppNotificationSettings: AppleNotificationSetting.disabled,
);

/// Captures what FCMService reports instead of shipping it.
class _FakeTelemetry extends TelemetryService {
  _FakeTelemetry()
      : super(appName: 'test', baseUrl: 'http://localhost', storage: SecureStorageService());

  final List<String> warnings = [];
  final List<Map<String, dynamic>?> attributes = [];

  @override
  void log(
    TelemetryLevel level,
    String message, {
    Object? error,
    StackTrace? stackTrace,
    Map<String, dynamic>? attributes,
  }) {
    if (level == TelemetryLevel.warning) {
      warnings.add(message);
      this.attributes.add(attributes);
    }
  }
}

const _denied = NotificationSettings(
  alert: AppleNotificationSetting.disabled,
  announcement: AppleNotificationSetting.disabled,
  authorizationStatus: AuthorizationStatus.denied,
  badge: AppleNotificationSetting.disabled,
  carPlay: AppleNotificationSetting.disabled,
  lockScreen: AppleNotificationSetting.disabled,
  notificationCenter: AppleNotificationSetting.disabled,
  showPreviews: AppleShowPreviewSetting.never,
  timeSensitive: AppleNotificationSetting.disabled,
  criticalAlert: AppleNotificationSetting.disabled,
  sound: AppleNotificationSetting.disabled,
  providesAppNotificationSettings: AppleNotificationSetting.disabled,
);

FCMService _service(_FakeMessaging messaging, {TelemetryService? telemetry}) {
  final container = ProviderContainer();
  addTearDown(container.dispose);
  final provider = Provider<FCMService>(
    (ref) => FCMService(
      ref: ref,
      storage: SecureStorageService(),
      messaging: messaging,
      telemetry: telemetry,
      apnsRetryDelay: Duration.zero,
    ),
  );
  return container.read(provider);
}

void main() {
  tearDown(() => debugDefaultTargetPlatformOverride = null);

  group('iOS APNs readiness', () {
    setUp(() => debugDefaultTargetPlatformOverride = TargetPlatform.iOS);

    test('waits for the APNs token before joining a topic', () async {
      final messaging = _FakeMessaging(apnsAfter: 3);

      await _service(messaging).subscribeToGlobal();

      expect(messaging.apnsPolls, 4);
      expect(messaging.subscribed, ['global']);
    });

    test('gym and global subscriptions share one APNs wait', () async {
      final messaging = _FakeMessaging(apnsAfter: 2);
      final fcm = _service(messaging);

      await Future.wait([fcm.subscribeToGlobal(), fcm.subscribeToGym('g1')]);

      expect(messaging.apnsPolls, 3);
      expect(messaging.subscribed, unorderedEquals(['global', 'gym_g1']));
    });

    test('gives up quietly when APNs never delivers a token', () async {
      final messaging = _FakeMessaging();
      final fcm = _service(messaging);

      await fcm.initialize();
      await fcm.subscribeToGlobal();

      expect(messaging.tokenRequested, isFalse);
      expect(messaging.subscribed, isEmpty);
    });

    test('reports a denied permission to telemetry', () async {
      final telemetry = _FakeTelemetry();
      final messaging = _FakeMessaging(apnsAfter: 0, permission: _denied);

      await _service(messaging, telemetry: telemetry).initialize();

      expect(messaging.tokenRequested, isFalse);
      expect(telemetry.warnings.single, contains('permission'));
      expect(telemetry.warnings.single, contains('denied'));
      expect(telemetry.attributes.single?['push.step'], 'permission');
    });

    test('reports a missing APNs token to telemetry', () async {
      final telemetry = _FakeTelemetry();

      await _service(_FakeMessaging(), telemetry: telemetry).initialize();

      expect(telemetry.warnings.single, contains('apns_token'));
      expect(telemetry.attributes.single?['push.platform'], 'iOS');
    });

    test('retries the wait on the next call after a failed one', () async {
      final messaging = _FakeMessaging(apnsAfter: 30);
      final fcm = _service(messaging);

      await fcm.subscribeToGlobal();
      expect(messaging.subscribed, isEmpty);

      await fcm.subscribeToGlobal();
      expect(messaging.subscribed, ['global']);
    });
  });

  test('Android does not wait for APNs', () async {
    debugDefaultTargetPlatformOverride = TargetPlatform.android;
    final messaging = _FakeMessaging();

    await _service(messaging).subscribeToGym('g1');

    expect(messaging.apnsPolls, 0);
    expect(messaging.subscribed, ['gym_g1']);
  });
}
