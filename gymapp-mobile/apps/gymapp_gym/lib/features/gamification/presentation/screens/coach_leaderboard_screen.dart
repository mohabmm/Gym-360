import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/gamification_provider.dart';

class CoachLeaderboardScreen extends ConsumerWidget {
  const CoachLeaderboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final leaderboard = ref.watch(trainerLeaderboardProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'لوحة المدربين' : 'Trainer Leaderboard'),
        actions: [
          TextButton(
            onPressed: () => context.push(RoutePaths.moderateTestimonials),
            child: Text(isArabic ? 'الشهادات' : 'Testimonials'),
          ),
        ],
      ),
      body: FeatureGate(
        featureCode: FeatureCodes.coachLeaderboard,
        child: leaderboard.when(
          data: (entries) {
            if (entries.isEmpty) {
              return EmptyState(
                icon: Icons.leaderboard_outlined,
                title: isArabic ? 'لا توجد بيانات' : 'No data yet',
              );
            }
            return RefreshIndicator(
              onRefresh: () => ref.refresh(trainerLeaderboardProvider.future),
              child: ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: entries.length,
                itemBuilder: (context, index) {
                  final entry = entries[index];
                  final isTop3 = index < 3;
                  return AppCard(
                    margin: const EdgeInsets.only(bottom: 10),
                    onTap: () =>
                        context.push('/gamification/ratings/${entry.trainerId}'),
                    child: Row(
                      children: [
                        // Rank badge
                        Container(
                          width: 36,
                          height: 36,
                          decoration: BoxDecoration(
                            color: isTop3
                                ? _rankColor(index).withOpacity(0.15)
                                : Colors.grey.shade100,
                            shape: BoxShape.circle,
                          ),
                          child: Center(
                            child: isTop3
                                ? Icon(Icons.emoji_events,
                                    color: _rankColor(index), size: 20)
                                : Text(
                                    '${index + 1}',
                                    style: theme.textTheme.titleSmall?.copyWith(
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        AvatarWidget(
                          imageUrl: entry.trainerPhotoUrl,
                          name: entry.trainerName,
                          size: 44,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                entry.trainerName,
                                style: theme.textTheme.titleSmall?.copyWith(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              Row(
                                children: [
                                  const Icon(Icons.star,
                                      size: 14, color: AppColors.star),
                                  const SizedBox(width: 4),
                                  Text(
                                    entry.averageRating.toStringAsFixed(1),
                                    style: theme.textTheme.bodySmall,
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    '${entry.sessionsThisMonth} ${isArabic ? 'حصة' : 'sessions'}',
                                    style: theme.textTheme.bodySmall,
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                        Text(
                          entry.normalizedScore.toStringAsFixed(0),
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => AppErrorWidget(
            message: isArabic ? 'خطأ في التحميل' : 'Error loading leaderboard',
            onRetry: () => ref.invalidate(trainerLeaderboardProvider),
          ),
        ),
      ),
    );
  }

  Color _rankColor(int index) {
    switch (index) {
      case 0:
        return AppColors.gold;
      case 1:
        return Colors.grey;
      case 2:
        return const Color(0xFFCD7F32);
      default:
        return Colors.grey;
    }
  }
}
