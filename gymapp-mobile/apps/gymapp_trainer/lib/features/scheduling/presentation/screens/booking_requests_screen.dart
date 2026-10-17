import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../../../home/presentation/providers/trainer_dashboard_provider.dart';
import '../providers/scheduling_providers.dart';

class BookingRequestsScreen extends ConsumerWidget {
  const BookingRequestsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final requestsAsync = ref.watch(pendingBookingRequestsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'طلبات الحجز' : 'Booking Requests'),
      ),
      body: RefreshIndicator(
        onRefresh: () async =>
            ref.invalidate(pendingBookingRequestsProvider),
        child: requestsAsync.when(
          data: (requests) {
            if (requests.isEmpty) {
              return EmptyState(
                icon: Icons.pending_actions,
                title: isArabic
                    ? 'لا توجد طلبات معلقة'
                    : 'No pending requests',
                subtitle: isArabic
                    ? 'ستظهر طلبات الحجز الجديدة هنا'
                    : 'New booking requests will appear here',
              );
            }

            final names = ref.watch(clientNameByIdProvider);
            return ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: requests.length,
              itemBuilder: (context, index) {
                final session = requests[index];
                return _BookingRequestCard(
                  session: session,
                  isArabic: isArabic,
                  memberName: names[session.memberId],
                  onAccept: () => _respond(
                    context, ref, session.id, 'accepted', isArabic),
                  onDecline: () => _respond(
                    context, ref, session.id, 'declined', isArabic),
                );
              },
            );
          },
          loading: () => ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: 5,
            itemBuilder: (_, __) => const Padding(
              padding: EdgeInsets.only(bottom: 8),
              child: ShimmerCard(height: 120),
            ),
          ),
          error: (e, _) => AppErrorWidget(
            message: e.toString(),
            onRetry: () => ref.invalidate(pendingBookingRequestsProvider),
          ),
        ),
      ),
    );
  }

  Future<void> _respond(
    BuildContext context,
    WidgetRef ref,
    String sessionId,
    String action,
    bool isArabic,
  ) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      if (action == 'accepted' || action == 'confirm') {
        await apiClient.postVoid(
          '${ApiEndpoints.sessionById(SessionId(sessionId))}/confirm',
        );
      } else {
        final trainerId = ref.read(authProvider).user?.id;
        if (trainerId == null) {
          throw Exception('No authenticated trainer');
        }
        await apiClient.postVoid(
          '${ApiEndpoints.sessionById(SessionId(sessionId))}/cancel',
          data: {'cancelledBy': trainerId, 'reason': 'declined by trainer'},
        );
      }
      ref.invalidate(pendingBookingRequestsProvider);
      // An accepted request becomes a scheduled session — without these the
      // Schedule tab and Home's "today" list kept showing the stale state.
      ref.invalidate(upcomingSessionsProvider);
      ref.invalidate(trainerDashboardProvider);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    }
  }
}

class _BookingRequestCard extends StatelessWidget {
  final SessionModel session;
  final bool isArabic;
  final String? memberName;
  final VoidCallback onAccept;
  final VoidCallback onDecline;

  const _BookingRequestCard({
    required this.session,
    required this.isArabic,
    required this.onAccept,
    required this.onDecline,
    this.memberName,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dateStr = DateFormat('dd/MM/yyyy').format(session.scheduledAt);
    final timeStr = DateFormat('hh:mm a', isArabic ? 'ar' : 'en').format(session.scheduledAt);
    final displayName = (memberName != null && memberName!.isNotEmpty)
        ? memberName!
        : (isArabic ? 'عميل' : 'Client');

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                AvatarWidget(
                  name: displayName,
                  size: AppConstants.avatarSizeSmall,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        displayName,
                        style: theme.textTheme.titleSmall
                            ?.copyWith(fontWeight: FontWeight.w600),
                      ),
                      Text(
                        '$dateStr • $timeStr',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              '${session.durationMinutes} ${isArabic ? 'دقيقة' : 'minutes'}',
              style: theme.textTheme.bodySmall?.copyWith(
                color: AppColors.grey500,
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: AppButton.outlined(
                    text: isArabic ? 'رفض' : 'Decline',
                    onPressed: onDecline,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: AppButton(
                    text: isArabic ? 'قبول' : 'Accept',
                    onPressed: onAccept,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
