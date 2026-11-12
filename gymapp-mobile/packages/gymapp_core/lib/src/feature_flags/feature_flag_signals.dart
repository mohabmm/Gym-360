import 'dart:async';

/// App-wide signal raised by the network layer when the API answers
/// `403 FEATURE_DISABLED:<key>` (or a legacy plan-gating error). The flags
/// controller listens and refreshes, so the feature disappears from the UI
/// instead of the user seeing an error.
abstract final class FeatureFlagSignals {
  static final _disabled = StreamController<String>.broadcast();

  static Stream<String> get featureDisabled => _disabled.stream;

  static void reportDisabled(String featureKey) => _disabled.add(featureKey);
}
