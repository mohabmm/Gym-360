import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../providers/communication_providers.dart';

/// Notifications inbox. Reached from the bell in the home app bar. Lists the
/// member's notifications (Ar/En), shows read state, lets them mark one or all
/// read, and deep-links to the relevant screen on tap. Refreshes live on
/// incoming FCM data messages.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final notificationsAsync = ref.watch(notificationsProvider);

    // Live refresh when a push arrives while the inbox is open.
    ref.listen(incomingNotificationProvider, (_, __) {
      ref.invalidate(notificationsProvider);
      ref.invalidate(unreadNotificationCountProvider);
    });

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الإشعارات' : 'Notifications'),
        actions: [
          TextButton(
            onPressed: () async {
              final repo = ref.read(notificationsRepositoryProvider);
              try {
                await repo.markAllNotificationsRead();
              } catch (_) {
                // Non-critical.
              }
              ref.invalidate(notificationsProvider);
              ref.invalidate(unreadNotificationCountProvider);
            },
            child: Text(isArabic ? 'تعليم الكل كمقروء' : 'Mark all read'),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(notificationsProvider);
          ref.invalidate(unreadNotificationCountProvider);
        },
        child: notificationsAsync.when(
          data: (notifications) {
            if (notifications.isEmpty) {
              return EmptyState(
                icon: Icons.notifications_none,
                title: isArabic ? 'لا توجد إشعارات' : 'No notifications',
                subtitle: isArabic
                    ? 'ستظهر التنبيهات والتذكيرات هنا'
                    : 'Alerts and reminders will appear here',
              );
            }
            return ListView.separated(
              itemCount: notifications.length,
              separatorBuilder: (_, __) => const Divider(height: 1),
              itemBuilder: (context, index) => _NotificationTile(
                notification: notifications[index],
                isArabic: isArabic,
              ),
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => AppErrorWidget(
            message: isArabic
                ? 'تعذر تحميل الإشعارات. يرجى المحاولة مرة أخرى.'
                : 'Could not load notifications. Please try again.',
            onRetry: () => ref.invalidate(notificationsProvider),
          ),
        ),
      ),
    );
  }
}

class _NotificationTile extends ConsumerWidget {
  final NotificationModel notification;
  final bool isArabic;

  const _NotificationTile(
      {required this.notification, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final title = isArabic && (notification.titleAr?.isNotEmpty ?? false)
        ? notification.titleAr!
        : notification.title;
    final body = isArabic && (notification.bodyAr?.isNotEmpty ?? false)
        ? notification.bodyAr!
        : notification.body;
    final unread = !notification.isRead;

    return ListTile(
      leading: CircleAvatar(
        backgroundColor:
            _iconColor(theme).withOpacity(unread ? 0.18 : 0.10),
        child: Icon(_iconFor(notification.type),
            color: _iconColor(theme), size: 20),
      ),
      title: Text(
        title,
        style: TextStyle(
          fontWeight: unread ? FontWeight.w700 : FontWeight.w500,
        ),
      ),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(body, maxLines: 2, overflow: TextOverflow.ellipsis),
          const SizedBox(height: 2),
          Text(
            _formatTime(context, notification.createdAt),
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.textTheme.bodySmall?.color?.withOpacity(0.6),
            ),
          ),
        ],
      ),
      trailing: unread
          ? Container(
              width: 10,
              height: 10,
              decoration: BoxDecoration(
                color: theme.colorScheme.primary,
                shape: BoxShape.circle,
              ),
            )
          : null,
      onTap: () => _handleTap(context, ref),
    );
  }

  Future<void> _handleTap(BuildContext context, WidgetRef ref) async {
    // Mark read first so the badge/list update even if navigation no-ops.
    if (!notification.isRead) {
      final repo = ref.read(notificationsRepositoryProvider);
      try {
        await repo.markNotificationRead(notification.id);
      } catch (_) {
        // Non-critical.
      }
      ref.invalidate(notificationsProvider);
      ref.invalidate(unreadNotificationCountProvider);
    }
    if (!context.mounted) return;
    final route = _routeFor(notification);
    if (route != null) context.go(route);
  }

  /// Deep-link target per notification type. Returns null when there is no
  /// dedicated screen (the tile just marks itself read).
  String? _routeFor(NotificationModel n) {
    switch (n.type) {
      case NotificationType.newMessage:
        return '/more/chat';
      case NotificationType.announcement:
        return '/more/announcements';
      case NotificationType.sessionReminder:
      case NotificationType.sessionBooked:
      case NotificationType.sessionCancelled:
      case NotificationType.bookingAccepted:
      case NotificationType.bookingRejected:
        return '/schedule';
      case NotificationType.workoutAssigned:
        return '/workouts';
      case NotificationType.membershipExpiring:
      case NotificationType.membershipExpired:
      case NotificationType.packageLow:
      case NotificationType.packageExhausted:
        return '/more/membership';
      default:
        return null;
    }
  }

  IconData _iconFor(String type) {
    switch (type) {
      case NotificationType.newMessage:
        return Icons.chat_bubble_outline;
      case NotificationType.announcement:
        return Icons.campaign_outlined;
      case NotificationType.sessionReminder:
      case NotificationType.sessionBooked:
      case NotificationType.bookingAccepted:
        return Icons.event_available_outlined;
      case NotificationType.sessionCancelled:
      case NotificationType.bookingRejected:
        return Icons.event_busy_outlined;
      case NotificationType.workoutAssigned:
        return Icons.fitness_center_outlined;
      case NotificationType.membershipExpiring:
      case NotificationType.membershipExpired:
      case NotificationType.packageLow:
      case NotificationType.packageExhausted:
        return Icons.card_membership_outlined;
      case NotificationType.paymentRecorded:
        return Icons.receipt_long_outlined;
      default:
        return Icons.notifications_outlined;
    }
  }

  Color _iconColor(ThemeData theme) => theme.colorScheme.primary;

  String _formatTime(BuildContext context, DateTime createdAt) {
    final local = createdAt.toLocal();
    final now = DateTime.now();
    final isToday = now.year == local.year &&
        now.month == local.month &&
        now.day == local.day;
    final locale = isArabic ? 'ar' : 'en';
    return isToday
        ? DateFormat('h:mm a', locale).format(local)
        : DateFormat('MMM d, h:mm a', locale).format(local);
  }
}
