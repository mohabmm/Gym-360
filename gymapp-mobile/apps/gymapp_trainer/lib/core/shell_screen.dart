import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'providers.dart';
import 'router.dart';

/// Bottom navigation shell wrapping the 5 main tabs.
/// Home | Clients | Workouts | Schedule | More
class ShellScreen extends ConsumerWidget {
  final Widget child;

  const ShellScreen({super.key, required this.child});

  static int _calculateSelectedIndex(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;
    if (location.startsWith(AppRoutes.home)) return 0;
    if (location.startsWith(AppRoutes.clients)) return 1;
    if (location.startsWith(AppRoutes.workouts)) return 2;
    if (location.startsWith(AppRoutes.schedule)) return 3;
    if (location.startsWith(AppRoutes.more)) return 4;
    return 0;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selectedIndex = _calculateSelectedIndex(context);
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
      bottomNavigationBar: NavigationBar(
        selectedIndex: selectedIndex,
        onDestinationSelected: (index) {
          switch (index) {
            case 0:
              context.go(AppRoutes.home);
              break;
            case 1:
              context.go(AppRoutes.clients);
              break;
            case 2:
              context.go(AppRoutes.workouts);
              break;
            case 3:
              context.go(AppRoutes.schedule);
              break;
            case 4:
              context.go(AppRoutes.more);
              break;
          }
        },
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.home_outlined),
            selectedIcon: const Icon(Icons.home),
            label: isArabic ? 'الرئيسية' : 'Home',
          ),
          NavigationDestination(
            icon: const Icon(Icons.people_outline),
            selectedIcon: const Icon(Icons.people),
            label: isArabic ? 'العملاء' : 'Clients',
          ),
          NavigationDestination(
            icon: const Icon(Icons.fitness_center_outlined),
            selectedIcon: const Icon(Icons.fitness_center),
            label: isArabic ? 'التمارين' : 'Workouts',
          ),
          NavigationDestination(
            icon: const Icon(Icons.calendar_today_outlined),
            selectedIcon: const Icon(Icons.calendar_today),
            label: isArabic ? 'الجدول' : 'Schedule',
          ),
          NavigationDestination(
            icon: const Icon(Icons.more_horiz),
            selectedIcon: const Icon(Icons.more_horiz),
            label: isArabic ? 'المزيد' : 'More',
          ),
        ],
      ),
    );
  }
}
