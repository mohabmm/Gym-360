import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../features/communication/presentation/providers/communication_providers.dart';
import 'router.dart';

/// Notifications whose tap opens the screen in their `route` (pt.starter_plan: the member's profile).
const _routedTypes = {'new_client_assigned', 'plan_programs_unlocked'};

/// Where a tapped notification with [data] goes in the trainer app, or null to stay put.
String? trainerPushRoute(Map<String, dynamic>? data) {
  final route = data?['route'];
  if (!_routedTypes.contains(data?['type']) || route is! String || !route.startsWith('/clients/')) return null;
  return route;
}

/// Opens the screen of a system notification the coach tapped (app in the background or closed).
class TrainerPushTapRouter extends ConsumerWidget {
  final Widget child;

  const TrainerPushTapRouter({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen<AsyncValue<Map<String, dynamic>>>(incomingNotificationProvider, (_, next) {
      final data = next.valueOrNull;
      final route = data?['_tapped'] == true ? trainerPushRoute(data) : null;
      if (route != null) ref.read(routerProvider).push(route);
    });
    return child;
  }
}
