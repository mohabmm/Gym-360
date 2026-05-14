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

class _BottomNav extends ConsumerWidget {
  final bool isArabic;

  const _BottomNav({required this.isArabic});

  int _currentIndex(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;
    if (location.startsWith('/workouts')) return 1;
    if (location.startsWith('/progress')) return 2;
    if (location.startsWith('/schedule')) return 3;
    if (location.startsWith('/more')) return 4;
    return 0;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final currentIndex = _currentIndex(context);

    return NavigationBar(
      selectedIndex: currentIndex,
      onDestinationSelected: (index) {
        switch (index) {
          case 0:
            context.go(AppRoutes.home);
          case 1:
            context.go(AppRoutes.workouts);
          case 2:
            context.go(AppRoutes.progress);
          case 3:
            context.go(AppRoutes.schedule);
          case 4:
            context.go(AppRoutes.more);
        }
      },
      destinations: [
        NavigationDestination(
          icon: const Icon(Icons.home_outlined),
          selectedIcon: const Icon(Icons.home),
          label: isArabic ? 'الرئيسية' : 'Home',
        ),
        NavigationDestination(
          icon: const Icon(Icons.fitness_center_outlined),
          selectedIcon: const Icon(Icons.fitness_center),
          label: isArabic ? 'التمارين' : 'Workout',
        ),
        NavigationDestination(
          icon: FeatureGateIcon(
            featureCode: FeatureCodes.progressTracking,
            icon: const Icon(Icons.trending_up_outlined),
            lockedIcon: const Icon(Icons.trending_up_outlined),
          ),
          selectedIcon: const Icon(Icons.trending_up),
          label: isArabic ? 'التقدم' : 'Progress',
        ),
        NavigationDestination(
          icon: FeatureGateIcon(
            featureCode: FeatureCodes.sessionBooking,
            icon: const Icon(Icons.calendar_today_outlined),
            lockedIcon: const Icon(Icons.calendar_today_outlined),
          ),
          selectedIcon: const Icon(Icons.calendar_today),
          label: isArabic ? 'المواعيد' : 'Schedule',
        ),
        NavigationDestination(
          icon: const Icon(Icons.more_horiz_outlined),
          selectedIcon: const Icon(Icons.more_horiz),
          label: isArabic ? 'المزيد' : 'More',
        ),
      ],
    );
  }
}
