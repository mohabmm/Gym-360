import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// Records which plugin methods were invoked; everything is a no-op.
class _FakePlugin implements FlutterLocalNotificationsPlugin {
  final List<Symbol> calls = [];
  Map<Symbol, dynamic>? lastShow;

  @override
  dynamic noSuchMethod(Invocation invocation) {
    calls.add(invocation.memberName);
    if (invocation.memberName == #show) lastShow = invocation.namedArguments;
    if (invocation.memberName == #resolvePlatformSpecificImplementation) {
      return null;
    }
    return Future<bool?>.value(true);
  }
}

const _visible = RemoteMessage(
  notification: RemoteNotification(title: 'حصة', body: 'Session in 1h'),
  data: {'type': 'session_reminder', 'sessionId': '42'},
);

void main() {
  late _FakePlugin plugin;
  late ForegroundNotificationPresenter presenter;

  setUp(() {
    plugin = _FakePlugin();
    presenter = ForegroundNotificationPresenter(plugin: plugin);
  });

  tearDown(() => debugDefaultTargetPlatformOverride = null);

  group('decodePayload', () {
    test('round-trips an FCM data map', () {
      expect(
        ForegroundNotificationPresenter.decodePayload('{"type":"x","id":"1"}'),
        {'type': 'x', 'id': '1'},
      );
    });

    test('null, empty, malformed and non-map payloads decode to {}', () {
      for (final p in [null, '', '{oops', '[1,2]']) {
        expect(ForegroundNotificationPresenter.decodePayload(p), isEmpty);
      }
    });
  });

  test('show is a no-op before initialize', () async {
    await presenter.show(_visible);
    expect(plugin.calls, isEmpty);
  });

  test('iOS: initialize and show never touch the plugin (native banner)',
      () async {
    debugDefaultTargetPlatformOverride = TargetPlatform.iOS;
    await presenter.initialize(onTap: (_) {});
    await presenter.show(_visible);
    expect(plugin.calls, isEmpty);
  });

  group('Android', () {
    setUp(() => debugDefaultTargetPlatformOverride = TargetPlatform.android);

    test('shows visible pushes on the shared channel with data payload',
        () async {
      await presenter.initialize(onTap: (_) {});
      await presenter.show(_visible);

      expect(plugin.lastShow, isNotNull);
      expect(plugin.lastShow![#title], 'حصة');
      expect(plugin.lastShow![#body], 'Session in 1h');
      final details = plugin.lastShow![#notificationDetails] as NotificationDetails;
      expect(details.android!.channelId, ForegroundNotificationPresenter.channelId);
      expect(
        ForegroundNotificationPresenter.decodePayload(plugin.lastShow![#payload] as String),
        {'type': 'session_reminder', 'sessionId': '42'},
      );
    });

    test('data-only (silent) pushes are not displayed', () async {
      await presenter.initialize(onTap: (_) {});
      await presenter.show(const RemoteMessage(data: {'type': 'config_refresh'}));
      expect(plugin.calls, isNot(contains(#show)));
    });

    test('initialize is idempotent', () async {
      await presenter.initialize(onTap: (_) {});
      await presenter.initialize(onTap: (_) {});
      expect(plugin.calls.where((c) => c == #initialize), hasLength(1));
    });
  });
}
