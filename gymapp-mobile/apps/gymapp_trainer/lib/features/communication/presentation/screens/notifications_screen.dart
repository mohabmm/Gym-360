import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../providers/communication_providers.dart';

/// Trainer notifications inbox. Opened from the bell on the home app bar.
/// Lists notifications (Ar/En), shows read state, supports mark-one / mark-all
/// read, deep-links on tap, and refreshes live on incoming FCM messages.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final notificationsAsync = ref.watch(notificationsProvider);

    ref.listen(incomingNotificationProvider, (_, __) {
      ref.invalidate(notificationsProvider);
      ref.invalidate(unreadNotificationCountProvider);
    });

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الإشعارات' : 'Notifications'),
        actions: [
          TextButton(
            onPressed: () => markAllTrainerNotificationsRead(ref),
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
            theme.colorScheme.primary.withOpacity(unread ? 0.18 : 0.10),
        child: Icon(_iconFor(notification.type),
            color: theme.colorScheme.primary, size: 20),
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
            _formatTime(notification.createdAt),
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
      onTap: () async {
        if (!notification.isRead) {
          await markTrainerNotificationRead(ref, notification.id);
        }
        if (!context.mounted) return;
        final route = _routeFor(notification);
        if (route != null) context.push(route);
      },
    );
  }

  /// Deep-link target per type. Null = no dedicated screen (just marks read).
  String? _routeFor(NotificationModel n) {
    switch (n.type) {
      case NotificationType.newMessage:
        return '/more/chat';
      case NotificationType.bookingRequest:
        return '/schedule/booking-requests';
      case NotificationType.sessionReminder:
      case NotificationType.sessionBooked:
      case NotificationType.sessionCancelled:
        return '/schedule';
      case NotificationType.rateTrainer:
        return '/more/ratings';
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
      case NotificationType.bookingRequest:
      case NotificationType.sessionBooked:
      case NotificationType.sessionReminder:
        return Icons.event_available_outlined;
      case NotificationType.sessionCancelled:
        return Icons.event_busy_outlined;
      case NotificationType.rateTrainer:
        return Icons.star_outline;
      case NotificationType.paymentRecorded:
        return Icons.receipt_long_outlined;
      default:
        return Icons.notifications_outlined;
    }
  }

  String _formatTime(DateTime createdAt) {
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
