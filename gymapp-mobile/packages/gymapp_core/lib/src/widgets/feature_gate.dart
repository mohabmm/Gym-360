import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/plan_features_model.dart';
import '../constants/feature_codes.dart';
import 'loading_shimmer.dart';
import 'upgrade_prompt.dart';

/// Provider for plan features -- fetched once and cached.
/// Invalidated by FCM config_refresh push notification.
/// This provider must be overridden in each app's ProviderScope
/// with the actual implementation that fetches from the API.
final planFeaturesProvider = FutureProvider<PlanFeaturesModel>((ref) async {
  // This is a placeholder. Each app overrides this with real API call.
  throw UnimplementedError(
    'planFeaturesProvider must be overridden with the actual billing repository',
  );
});

/// Reusable feature gate widget.
/// Wraps any screen/widget to enforce plan-based feature gating.
///
/// When planFeaturesProvider is invalidated by push signal and re-fetches,
/// this widget automatically rebuilds: shows child if enabled, shows
/// UpgradePrompt if not.
///
/// Fail-open: if features cannot be checked (error state), shows the child
/// to avoid blocking users due to transient failures.
class FeatureGate extends ConsumerWidget {
  /// The feature code to check (from FeatureCodes).
  final String featureCode;

  /// The minimum plan required for this feature.
  final String? requiredPlan;

  /// The widget to show when the feature is enabled.
  final Widget child;

  /// Optional custom locked widget instead of default UpgradePrompt.
  final Widget? lockedWidget;

  /// Whether to show a shimmer while loading (vs transparent).
  final bool showLoadingShimmer;

  const FeatureGate({
    super.key,
    required this.featureCode,
    this.requiredPlan,
    required this.child,
    this.lockedWidget,
    this.showLoadingShimmer = true,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final features = ref.watch(planFeaturesProvider);

    return features.when(
      data: (planFeatures) {
        if (planFeatures.isEnabled(featureCode)) {
          return child;
        }
        return lockedWidget ??
            UpgradePrompt(
              featureCode: featureCode,
              requiredPlan:
                  requiredPlan ?? FeatureCodes.requiredPlanFor(featureCode),
            );
      },
      loading: () => showLoadingShimmer
          ? const ShimmerCard(height: 200)
          : const SizedBox.shrink(),
      // Fail-open: show feature if can't check
      error: (_, __) => child,
    );
  }
}

/// Gate that shows a lock icon on navigation items for gated features.
/// Tapping opens the upgrade prompt instead of navigating.
class FeatureGateIcon extends ConsumerWidget {
  final String featureCode;
  final Widget icon;
  final Widget? lockedIcon;

  const FeatureGateIcon({
    super.key,
    required this.featureCode,
    required this.icon,
    this.lockedIcon,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final features = ref.watch(planFeaturesProvider);

    return features.when(
      data: (planFeatures) {
        if (planFeatures.isEnabled(featureCode)) {
          return icon;
        }
        return Stack(
          children: [
            lockedIcon ?? icon,
            Positioned(
              right: -2,
              top: -2,
              child: Container(
                padding: const EdgeInsets.all(2),
                decoration: BoxDecoration(
                  color: Colors.orange,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: const Icon(
                  Icons.lock,
                  size: 10,
                  color: Colors.white,
                ),
              ),
            ),
          ],
        );
      },
      loading: () => icon,
      error: (_, __) => icon,
    );
  }
}
