import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/communication_providers.dart';

/// App-bar bell that shows the unread-notification count and opens the
/// notifications inbox. Refreshes its badge live on incoming FCM messages.
class NotificationBell extends ConsumerWidget {
  const NotificationBell({super.key, this.route = '/more/notifications'});

  final String route;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final countAsync = ref.watch(unreadNotificationCountProvider);
    final count = countAsync.valueOrNull ?? 0;

    // Bump the badge live when a push arrives.
    ref.listen(incomingNotificationProvider, (_, __) {
      ref.invalidate(unreadNotificationCountProvider);
    });

    return Stack(
      alignment: Alignment.center,
      children: [
        IconButton(
          icon: const Icon(Icons.notifications_outlined),
          tooltip: isArabic ? 'الإشعارات' : 'Notifications',
          onPressed: () => context.go(route),
        ),
        if (count > 0)
          Positioned(
            top: 8,
            right: 6,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
              constraints: const BoxConstraints(minWidth: 18, minHeight: 18),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.error,
                borderRadius: BorderRadius.circular(9),
              ),
              child: Text(
                count > 99 ? '99+' : '$count',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
      ],
    );
  }
}
