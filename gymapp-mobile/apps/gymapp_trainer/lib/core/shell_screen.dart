import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart' show FeatureFlagKeys, featureFlagsProvider;

import 'providers.dart';
import 'router.dart';

/// Bottom navigation shell. Tabs come from the gym's feature flags:
/// Home | Clients | Workouts | Schedule | More (PT gyms: Home | Clients | More).
class ShellScreen extends ConsumerWidget {
  final Widget child;

  const ShellScreen({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    // Cold start: the stored token is still being validated via /profile.
    // Rendering the tabs now would show a nameless greeting and zeroed stats
    // (no user / gym yet), so hold a loader until auth resolves.
    final authResolving = ref.watch(
      authProvider.select((s) => s.status == AuthStatus.initial),
    );

    return Scaffold(
      body: authResolving
          ? const Center(child: CircularProgressIndicator())
          : child,
      bottomNavigationBar: _TrainerNavBar(isArabic: isArabic),
    );
  }
}

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
  _Tab(AppRoutes.clients, null, Icons.people_outline, Icons.people, 'العملاء', 'Clients'),
  _Tab(AppRoutes.workouts, FeatureFlagKeys.trainingWorkouts, Icons.fitness_center_outlined, Icons.fitness_center, 'التمارين', 'Workouts'),
  _Tab(AppRoutes.schedule, FeatureFlagKeys.schedulingBooking, Icons.calendar_today_outlined, Icons.calendar_today, 'الجدول', 'Schedule'),
  _Tab(AppRoutes.more, null, Icons.more_horiz, Icons.more_horiz, 'المزيد', 'More'),
];

/// Bottom navigation built only from enabled tabs (PT gyms: Home · Clients · More).
class _TrainerNavBar extends ConsumerWidget {
  final bool isArabic;
  const _TrainerNavBar({required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final flags = ref.watch(featureFlagsProvider);
    final tabs = _tabs.where((t) => t.flag == null || flags.isEnabled(t.flag!)).toList();
    final location = GoRouterState.of(context).matchedLocation;
    final current = tabs.lastIndexWhere((t) => location.startsWith(t.path));

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
