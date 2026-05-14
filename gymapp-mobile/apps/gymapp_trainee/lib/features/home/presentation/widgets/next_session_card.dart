import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../communication/presentation/providers/communication_providers.dart';
import '../providers/home_providers.dart';

class NextSessionCard extends ConsumerWidget {
  const NextSessionCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final sessionsAsync = ref.watch(upcomingSessionsProvider);
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return sessionsAsync.when(
      data: (sessions) {
        if (sessions.isEmpty) return const SizedBox.shrink();

        final next = sessions.first;
        final isToday = DateUtils.isSameDay(next.scheduledAt, DateTime.now());
        final memberships =
            ref.watch(authProvider).valueOrNull?.user?.gymMemberships ??
                const [];
        final trainerMatch = memberships.firstWhere(
          (m) => m.assignedTrainerId == next.trainerId,
          orElse: () => const GymMembershipModel(gymId: ''),
        );
        final trainerName = trainerMatch.assignedTrainerName?.trim();
        final withTrainerLabel = (trainerName == null || trainerName.isEmpty)
            ? (isArabic ? 'مع المدرب' : 'with Trainer')
            : (isArabic ? 'مع $trainerName' : 'with $trainerName');

        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: InkWell(
            onTap: () => context.go(AppRoutes.schedule),
            borderRadius: BorderRadius.circular(AppConstants.borderRadius),
            child: Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: theme.cardColor,
                borderRadius:
                    BorderRadius.circular(AppConstants.borderRadius),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.04),
                    blurRadius: 8,
                    offset: const Offset(0, 2),
                  ),
                ],
              ),
              child: Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: isToday
                          ? theme.colorScheme.primary.withOpacity(0.1)
                          : theme.colorScheme.secondary.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(
                      Icons.calendar_today,
                      color: isToday
                          ? theme.colorScheme.primary
                          : theme.colorScheme.secondary,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isArabic ? 'الجلسة القادمة' : 'Next Session',
                          style: theme.textTheme.bodySmall?.copyWith(
                            color:
                                theme.colorScheme.onSurface.withOpacity(0.5),
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          withTrainerLabel,
                          style: theme.textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        isToday
                            ? (isArabic ? 'اليوم' : 'Today')
                            : DateFormatter.formatLong(
                                next.scheduledAt,
                                locale: isArabic ? 'ar' : 'en',
                              ),
                        style: theme.textTheme.bodySmall?.copyWith(
                          fontWeight: FontWeight.w600,
                          color: isToday
                              ? theme.colorScheme.primary
                              : null,
                        ),
                      ),
                      Text(
                        DateFormatter.formatTime(
                          next.scheduledAt,
                          locale: isArabic ? 'ar' : 'en',
                        ),
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(width: 4),
                  IconButton(
                    icon: const Icon(Icons.chat_bubble_outline),
                    tooltip: isArabic ? 'محادثة' : 'Chat',
                    color: theme.colorScheme.primary,
                    onPressed: () =>
                        _startChat(context, ref, next.trainerId),
                  ),
                  Icon(
                    Icons.chevron_right,
                    color: theme.colorScheme.onSurface.withOpacity(0.3),
                  ),
                ],
              ),
            ),
          ),
        );
      },
      loading: () => const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: ShimmerCard(height: 80),
      ),
      error: (_, __) => const SizedBox.shrink(),
    );
  }

  Future<void> _startChat(
      BuildContext context, WidgetRef ref, String trainerId) async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final auth = ref.read(authProvider).valueOrNull;
    final gymId = auth?.currentGymId;
    if (gymId == null) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic ? 'لا يوجد جيم نشط' : 'No active gym'),
      ));
      return;
    }
    try {
      final apiClient = ref.read(apiClientProvider);
      final conversationId = await apiClient.post<String>(
        ApiEndpoints.conversations,
        data: {'gymId': gymId, 'participant2': trainerId},
        parser: (data) => data.toString(),
      );
      ref.read(activeConversationIdProvider.notifier).state =
          conversationId;
      ref.invalidate(conversationsProvider);
      if (!context.mounted) return;
      context.go(AppRoutes.chat);
    } catch (_) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'تعذر بدء المحادثة. حاول مرة أخرى.'
            : 'Could not start chat. Try again.'),
      ));
    }
  }
}
