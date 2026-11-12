import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

class _MemoryCache implements FeatureFlagCache {
  Map<String, dynamic>? stored;
  _MemoryCache([this.stored]);

  @override
  Future<Map<String, dynamic>?> read() async => stored;

  @override
  Future<void> write(Map<String, dynamic> json) async => stored = json;

  @override
  Future<void> clear() async => stored = null;
}

Map<String, dynamic> _server(Map<String, bool> flags, {String preset = 'pt_module'}) =>
    {'gymId': 'g1', 'preset': preset, 'flags': flags, 'refreshAfterSeconds': 300};

void main() {
  group('ApiFeatureFlagService fail-safe order', () {
    test('no cache and no network → pt_module defaults, never all on', () async {
      final service = ApiFeatureFlagService(fetch: () async => throw Exception('offline'), cache: _MemoryCache());

      await service.load();
      final flags = await service.refresh();

      expect(flags.origin, FeatureFlagsOrigin.defaults);
      expect(flags.isEnabled(FeatureFlagKeys.ptPackages), isTrue);
      expect(flags.isEnabled(FeatureFlagKeys.classes), isFalse);
      expect(flags.isEnabled(FeatureFlagKeys.nutrition), isFalse);
    });

    test('network failure keeps the last cached flags', () async {
      final cache = _MemoryCache(FeatureFlags(
        values: const {FeatureFlagKeys.classes: true},
        preset: 'business',
        origin: FeatureFlagsOrigin.server,
      ).toJson());
      final service = ApiFeatureFlagService(fetch: () async => throw Exception('500'), cache: cache);

      await service.load();
      final flags = await service.refresh();

      expect(flags.origin, FeatureFlagsOrigin.cache);
      expect(flags.isEnabled(FeatureFlagKeys.classes), isTrue);
    });

    test('successful fetch wins and is cached', () async {
      final cache = _MemoryCache();
      final service = ApiFeatureFlagService(
        fetch: () async => _server({FeatureFlagKeys.ptPackages: true, FeatureFlagKeys.nutrition: true}),
        cache: cache,
      );

      final flags = await service.refresh();

      expect(flags.origin, FeatureFlagsOrigin.server);
      expect(flags.isEnabled(FeatureFlagKeys.nutrition), isTrue);
      expect(cache.stored, isNotNull);
    });

    test('unknown keys are off', () async {
      final service = ApiFeatureFlagService(fetch: () async => _server({}), cache: _MemoryCache());
      final flags = await service.refresh();
      expect(flags.isEnabled('something.new'), isFalse);
    });

    test('clear returns to pt_module defaults', () async {
      final service = ApiFeatureFlagService(
        fetch: () async => _server({FeatureFlagKeys.classes: true}, preset: 'business'),
        cache: _MemoryCache(),
      );
      await service.refresh();

      await service.clear();

      expect(service.current.preset, 'pt_module');
      expect(service.current.isEnabled(FeatureFlagKeys.classes), isFalse);
    });

    test('emits changes only when values change', () async {
      var calls = 0;
      final service = ApiFeatureFlagService(
        fetch: () async {
          calls++;
          return _server({FeatureFlagKeys.nutrition: calls > 1});
        },
        cache: _MemoryCache(),
      );
      final emitted = <FeatureFlags>[];
      final sub = service.changes.listen(emitted.add);

      await service.refresh();
      await service.refresh();
      await service.refresh();
      await Future<void>.delayed(Duration.zero);

      expect(emitted.map((f) => f.isEnabled(FeatureFlagKeys.nutrition)), [false, true]);
      await sub.cancel();
    });
  });

  group('FeatureRouteGuard', () {
    const guard = FeatureRouteGuard(
      homePath: '/home',
      routeFlags: {
        '/more/nutrition': FeatureFlagKeys.nutrition,
        '/schedule/classes': FeatureFlagKeys.classes,
        '/schedule': FeatureFlagKeys.ptDualSessionLogging,
      },
    );
    final pt = FeatureFlags.ptDefaults();

    test('disabled route (and deep link with query) redirects home', () {
      expect(guard.redirect('/more/nutrition', pt), '/home');
      expect(guard.redirect('/more/nutrition/water?x=1', pt), '/home');
    });

    test('longest prefix wins', () {
      expect(guard.redirect('/schedule/classes', pt), '/home');
      expect(guard.redirect('/schedule/sessions/abc', pt), isNull);
    });

    test('unlisted routes are allowed', () {
      expect(guard.redirect('/settings', pt), isNull);
      expect(guard.redirect('/more-things', pt), isNull);
    });
  });

  group('visibleBy', () {
    test('drops items whose flag is off and keeps unflagged ones', () {
      final items = [
        const Flagged('home'),
        const Flagged('nutrition', flag: FeatureFlagKeys.nutrition),
        const Flagged('packages', flag: FeatureFlagKeys.ptPackages),
      ];

      expect(items.visibleBy(FeatureFlags.ptDefaults().isEnabled), ['home', 'packages']);
    });
  });
}
