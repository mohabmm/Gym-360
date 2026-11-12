import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'providers.dart';
import 'router.dart';

/// Shell screen providing bottom navigation and gym suspension overlay.
class ShellScreen extends ConsumerWidget {
  final Widget child;

  const ShellScreen({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isSuspended = ref.watch(gymSuspendedProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    if (isSuspended) {
      return const Scaffold(
        body: GymSuspensionOverlay(),
      );
    }

    return Scaffold(
      body: child,
      bottomNavigationBar: _BottomNav(isArabic: isArabic),
    );
  }
}

/// A bottom-nav tab: route, owning flag (null = always on), icons, labels.
class _Tab {
  final String path;
  final String? flag;
  final IconData icon;
  final IconData selectedIcon;
  final String ar;
  final String en;
  const _Tab(this.path, this.flag, this.icon, this.selectedIcon, this.ar, this.en);
}

const _tabs = [
  _Tab(AppRoutes.home, null, Icons.home_outlined, Icons.home, 'الرئيسية', 'Home'),
  _Tab(AppRoutes.workouts, FeatureFlagKeys.trainingWorkouts, Icons.fitness_center_outlined, Icons.fitness_center, 'التمارين', 'Workout'),
  _Tab(AppRoutes.progress, FeatureFlagKeys.ptProgressMeasurements, Icons.trending_up_outlined, Icons.trending_up, 'التقدم', 'Progress'),
  _Tab(AppRoutes.schedule, FeatureFlagKeys.ptDualSessionLogging, Icons.calendar_today_outlined, Icons.calendar_today, 'حصصي', 'Sessions'),
  _Tab(AppRoutes.more, null, Icons.more_horiz_outlined, Icons.more_horiz, 'المزيد', 'More'),
];

/// Bottom navigation built only from enabled tabs, so a PT gym sees
/// Home · Progress · Sessions · More with no empty slots.
class _BottomNav extends ConsumerWidget {
  final bool isArabic;

  const _BottomNav({required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final flags = ref.watch(featureFlagsProvider);
    final tabs = _tabs.where((t) => t.flag == null || flags.isEnabled(t.flag!)).toList();
    final location = GoRouterState.of(context).matchedLocation;
    final current = tabs.lastIndexWhere((t) => t.path != AppRoutes.home && location.startsWith(t.path));

    return NavigationBar(
      selectedIndex: current < 0 ? 0 : current,
      onDestinationSelected: (index) => context.go(tabs[index].path),
      destinations: [
        for (final t in tabs)
          NavigationDestination(
            icon: Icon(t.icon),
            selectedIcon: Icon(t.selectedIcon),
            label: isArabic ? t.ar : t.en,
          ),
      ],
    );
  }
}
