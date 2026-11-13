import 'dart:async';

import 'package:hive/hive.dart';

import 'feature_flags.dart';

/// The only way app code reads feature flags. Swap the implementation (e.g. a
/// self-hosted Flagsmith client) without touching screens.
abstract class FeatureFlagService {
  /// Latest known flags (cache or pt_module defaults before the first fetch).
  FeatureFlags get current;

  /// Emits whenever the flags change.
  Stream<FeatureFlags> get changes;

  /// Loads the cached flags (no network). Falls back to pt_module defaults.
  Future<FeatureFlags> load();

  /// Fetches from the server and caches. On failure keeps the last known flags.
  Future<FeatureFlags> refresh();

  /// Forgets cached flags (logout / gym switch) and returns to the defaults.
  Future<void> clear();

  void dispose();
}

/// Device cache for the last successful flags response.
abstract class FeatureFlagCache {
  Future<Map<String, dynamic>?> read();
  Future<void> write(Map<String, dynamic> json);
  Future<void> clear();
}

class HiveFeatureFlagCache implements FeatureFlagCache {
  static const boxName = 'feature_flags_box';
  static const _key = 'flags';

  Future<Box<dynamic>> _box() async {
    if (Hive.isBoxOpen(boxName)) return Hive.box(boxName);
    return Hive.openBox(boxName);
  }

  @override
  Future<Map<String, dynamic>?> read() async {
    final raw = (await _box()).get(_key);
    return raw is Map ? Map<String, dynamic>.from(raw) : null;
  }

  @override
  Future<void> write(Map<String, dynamic> json) async => (await _box()).put(_key, json);

  @override
  Future<void> clear() async => (await _box()).delete(_key);
}

/// Fetches `GET /api/v1/feature-flags/me` through [fetch] (returns the `data` map).
class ApiFeatureFlagService implements FeatureFlagService {
  final Future<Map<String, dynamic>> Function() _fetch;
  final FeatureFlagCache _cache;
  final _controller = StreamController<FeatureFlags>.broadcast();
  FeatureFlags _current = FeatureFlags.ptDefaults();
  Future<FeatureFlags>? _inFlight;

  ApiFeatureFlagService({
    required Future<Map<String, dynamic>> Function() fetch,
    FeatureFlagCache? cache,
  })  : _fetch = fetch,
        _cache = cache ?? HiveFeatureFlagCache();

  @override
  FeatureFlags get current => _current;

  @override
  Stream<FeatureFlags> get changes => _controller.stream;

  @override
  Future<FeatureFlags> load() async {
    try {
      final cached = await _cache.read();
      if (cached != null) _set(FeatureFlags.fromJson(cached, origin: FeatureFlagsOrigin.cache));
    } catch (_) {
      // Corrupt cache: keep defaults.
    }
    return _current;
  }

  @override
  Future<FeatureFlags> refresh() => _inFlight ??= _doRefresh().whenComplete(() => _inFlight = null);

  Future<FeatureFlags> _doRefresh() async {
    try {
      final flags = FeatureFlags.fromJson(await _fetch());
      await _cache.write(flags.toJson());
      _set(flags);
    } catch (_) {
      // Fail-safe: keep the last cache (or pt_module defaults). Never "all on".
    }
    return _current;
  }

  @override
  Future<void> clear() async {
    await _cache.clear();
    _set(FeatureFlags.ptDefaults());
  }

  void _set(FeatureFlags next) {
    final changed = next != _current;
    _current = next;
    if (changed && !_controller.isClosed) _controller.add(next);
  }

  @override
  void dispose() => _controller.close();
}
