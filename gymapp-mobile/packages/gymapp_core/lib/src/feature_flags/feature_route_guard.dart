import 'feature_flags.dart';

/// Maps route path prefixes to the flag that owns them, for go_router redirects.
/// Deep links go through the same redirect, so a disabled route is never shown.
class FeatureRouteGuard {
  /// Longest prefix wins. Paths not listed are always allowed.
  final Map<String, String> routeFlags;
  final String homePath;

  const FeatureRouteGuard({required this.routeFlags, required this.homePath});

  String? flagFor(String location) {
    final path = Uri.parse(location).path;
    String? bestPrefix;
    for (final prefix in routeFlags.keys.where((p) => _matches(path, p))) {
      if (prefix.length > (bestPrefix?.length ?? -1)) bestPrefix = prefix;
    }
    return bestPrefix == null ? null : routeFlags[bestPrefix];
  }

  static bool _matches(String path, String prefix) =>
      path == prefix || path.startsWith(prefix.endsWith('/') ? prefix : '$prefix/');

  /// `homePath` when [location] belongs to a disabled feature, otherwise null.
  String? redirect(String location, FeatureFlags flags) {
    final flag = flagFor(location);
    return flag != null && !flags.isEnabled(flag) ? homePath : null;
  }
}
