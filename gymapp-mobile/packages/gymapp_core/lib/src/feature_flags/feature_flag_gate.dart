import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'feature_flags_provider.dart';

/// Shows [child] only when [flag] is on. When off it renders nothing at all (no
/// placeholder, no gap) unless [fallback] is given.
class FeatureFlagGate extends ConsumerWidget {
  final String flag;
  final Widget child;
  final Widget? fallback;

  const FeatureFlagGate({super.key, required this.flag, required this.child, this.fallback});

  @override
  Widget build(BuildContext context, WidgetRef ref) =>
      ref.watch(isFeatureEnabledProvider(flag)) ? child : (fallback ?? const SizedBox.shrink());
}

/// An item that is visible only when its flag (if any) is on. Use with
/// [visibleByFlags] to build nav bars, menus and home sections without gaps.
class Flagged<T> {
  final String? flag;
  final T item;
  const Flagged(this.item, {this.flag});
}

extension VisibleByFlags<T> on Iterable<Flagged<T>> {
  List<T> visibleBy(bool Function(String key) isEnabled) =>
      where((f) => f.flag == null || isEnabled(f.flag!)).map((f) => f.item).toList();
}
