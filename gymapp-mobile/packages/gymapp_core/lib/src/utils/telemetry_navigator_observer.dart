import 'package:flutter/widgets.dart';

import '../services/telemetry_service.dart';

/// Tracks the current route name and feeds it to [TelemetryService] so every
/// log/error record carries the "where did it happen" signal. Attach it to the
/// app's GoRouter via `observers: [TelemetryNavigatorObserver(telemetry)]`.
class TelemetryNavigatorObserver extends NavigatorObserver {
  TelemetryNavigatorObserver(this._telemetry);

  final TelemetryService _telemetry;

  void _update(Route<dynamic>? route) {
    final name = _nameOf(route);
    if (name != null) _telemetry.currentRoute = name;
  }

  String? _nameOf(Route<dynamic>? route) {
    final settings = route?.settings;
    if (settings == null) return null;
    // GoRouter sets settings.name to the matched path/name when available.
    return settings.name ?? (settings.arguments == null ? null : route?.toString());
  }

  @override
  void didPush(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _update(route);

  @override
  void didReplace({Route<dynamic>? newRoute, Route<dynamic>? oldRoute}) =>
      _update(newRoute);

  @override
  void didPop(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _update(previousRoute);

  @override
  void didRemove(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _update(previousRoute);
}
