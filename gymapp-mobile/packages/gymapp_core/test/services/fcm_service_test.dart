import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// APNs token shows up only after [apnsAfter] polls (null = never).
class _FakeMessaging implements FirebaseMessaging {
  _FakeMessaging({this.apnsAfter});

  final int? apnsAfter;
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
  Future<String?> getToken({String? vapidKey}) async {
    tokenRequested = true;
    return 'fcm-token';
  }

  /// requestPermission is routed here so the fake needn't mirror its
  /// eight-parameter signature.
  @override
  dynamic noSuchMethod(Invocation invocation) {
    if (invocation.memberName == #requestPermission) {
      return Future.value(_authorized);
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

FCMService _service(_FakeMessaging messaging) {
  final container = ProviderContainer();
  addTearDown(container.dispose);
  final provider = Provider<FCMService>(
    (ref) => FCMService(
      ref: ref,
      storage: SecureStorageService(),
      messaging: messaging,
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
