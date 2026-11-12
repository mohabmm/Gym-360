import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'feature_flag_service.dart';
import 'feature_flag_signals.dart';
import 'feature_flags.dart';

/// Each app overrides this with an [ApiFeatureFlagService] wired to its ApiClient.
final featureFlagServiceProvider = Provider<FeatureFlagService>((ref) {
  throw UnimplementedError('featureFlagServiceProvider must be overridden by the app');
});

/// Current flags. Rebuilds every widget that watches it when flags change.
final featureFlagsProvider = StateNotifierProvider<FeatureFlagsController, FeatureFlags>((ref) {
  // Riverpod disposes the controller itself when the provider is disposed.
  return FeatureFlagsController(ref.watch(featureFlagServiceProvider));
});

/// Convenience: `ref.watch(isFeatureEnabledProvider('pt.packages'))`.
final isFeatureEnabledProvider = Provider.family<bool, String>(
  (ref, key) => ref.watch(featureFlagsProvider).isEnabled(key),
);

/// Keeps flags fresh: loads the cache at start, refreshes after login, when the app
/// returns to the foreground, and every [interval] while it is open.
class FeatureFlagsController extends StateNotifier<FeatureFlags> with WidgetsBindingObserver {
  static const interval = Duration(minutes: 5);

  final FeatureFlagService _service;
  StreamSubscription<FeatureFlags>? _sub;
  StreamSubscription<String>? _disabledSub;
  Timer? _timer;
  bool _active = false;

  FeatureFlagsController(this._service) : super(_service.current) {
    _sub = _service.changes.listen((flags) {
      if (mounted) state = flags;
    });
    // The API said a feature is off: refresh so it disappears from the UI.
    _disabledSub = FeatureFlagSignals.featureDisabled.listen((_) => refresh());
  }

  /// Call once at app start (before or after login).
  Future<void> start() async {
    state = await _service.load();
    if (_active) return;
    _active = true;
    WidgetsBinding.instance.addObserver(this);
    _timer = Timer.periodic(interval, (_) => refresh());
  }

  /// Fetch from the server now (after login, gym switch, FEATURE_DISABLED, resume).
  Future<void> refresh() async {
    final flags = await _service.refresh();
    if (mounted) state = flags;
  }

  /// Logout: forget this gym's flags.
  Future<void> reset() async {
    await _service.clear();
    if (mounted) state = _service.current;
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState lifecycle) {
    if (lifecycle == AppLifecycleState.resumed) refresh();
  }

  @override
  void dispose() {
    _timer?.cancel();
    _sub?.cancel();
    _disabledSub?.cancel();
    if (_active) WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }
}
