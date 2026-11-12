import 'feature_flag_keys.dart';

/// Where the current flag values came from.
enum FeatureFlagsOrigin {
  /// Fresh from `GET /feature-flags/me`.
  server,

  /// Last successful response, read from the device cache.
  cache,

  /// Never fetched and no cache: the pt_module defaults.
  defaults,
}

/// Immutable snapshot of the resolved flags for the signed-in user's gym.
class FeatureFlags {
  final Map<String, bool> values;
  final String preset;
  final FeatureFlagsOrigin origin;
  final DateTime? fetchedAt;

  const FeatureFlags({
    required this.values,
    required this.preset,
    required this.origin,
    this.fetchedAt,
  });

  /// Fail-safe fallback: pt_module, never "all on".
  factory FeatureFlags.ptDefaults() => FeatureFlags(
        values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: true},
        preset: 'pt_module',
        origin: FeatureFlagsOrigin.defaults,
      );

  /// Unknown keys are OFF.
  bool isEnabled(String key) => values[key] ?? false;

  bool allEnabled(Iterable<String> keys) => keys.every(isEnabled);

  FeatureFlags withOrigin(FeatureFlagsOrigin next) =>
      FeatureFlags(values: values, preset: preset, origin: next, fetchedAt: fetchedAt);

  /// Parses the `data` object of `GET /api/v1/feature-flags/me`.
  factory FeatureFlags.fromJson(Map<String, dynamic> json, {FeatureFlagsOrigin origin = FeatureFlagsOrigin.server}) {
    final raw = json['flags'];
    final values = <String, bool>{};
    if (raw is Map) {
      raw.forEach((key, value) {
        if (key is String && value is bool) values[key] = value;
      });
    }
    return FeatureFlags(
      values: values,
      preset: json['preset'] as String? ?? 'pt_module',
      origin: origin,
      fetchedAt: DateTime.tryParse(json['fetchedAt'] as String? ?? '') ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
        'flags': values,
        'preset': preset,
        'fetchedAt': (fetchedAt ?? DateTime.now()).toIso8601String(),
      };

  @override
  bool operator ==(Object other) =>
      other is FeatureFlags &&
      other.preset == preset &&
      other.values.length == values.length &&
      other.values.entries.every((e) => values[e.key] == e.value);

  @override
  int get hashCode => Object.hash(preset, Object.hashAllUnordered(values.entries.map((e) => '${e.key}=${e.value}')));
}
