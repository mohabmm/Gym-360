import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/gamification_provider.dart';

class MemberLeaderboardScreen extends ConsumerWidget {
  const MemberLeaderboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final leaderboard = ref.watch(memberLeaderboardProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'لوحة الاعضاء' : 'Member Leaderboard'),
      ),
      body: FeatureGate(
        featureCode: FeatureCodes.leaderboards,
        child: leaderboard.when(
          data: (entries) {
            if (entries.isEmpty) {
              return EmptyState(
                icon: Icons.leaderboard_outlined,
                title: isArabic ? 'لا توجد بيانات' : 'No data yet',
              );
            }
            return RefreshIndicator(
              onRefresh: () => ref.refresh(memberLeaderboardProvider.future),
              child: ListView.builder(
                padding: const EdgeInsets.all(16),
                itemCount: entries.length,
                itemBuilder: (context, index) {
                  final entry = entries[index];
                  return AppCard(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          SizedBox(
                            width: 32,
                            child: Center(
                              child: Text(
                                '${entry.position}',
                                style: theme.textTheme.titleSmall?.copyWith(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          AvatarWidget(
                            imageUrl: entry.memberPhotoUrl,
                            name: entry.memberName,
                            size: 40,
                          ),
                        ],
                      ),
                      title: Text(
                        entry.memberName,
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      trailing: Text(
                        _formatScore(entry.scoreLabel, entry.score, isArabic),
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                          color: theme.colorScheme.primary,
                        ),
                      ),
                    ),
                  );
                },
              ),
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => AppErrorWidget(
            message: isArabic ? 'خطأ في التحميل' : 'Error loading leaderboard',
            onRetry: () => ref.invalidate(memberLeaderboardProvider),
          ),
        ),
      ),
    );
  }

  String _formatScore(String? label, double score, bool isArabic) {
    final n = score.toInt();
    if (!isArabic) return label ?? '$n pts';
    if (label == null) return '$n نقطة';
    final lower = label.toLowerCase();
    if (lower.contains('session')) return '$n حصة';
    if (lower.contains('check') || lower.contains('visit') || lower.contains('attendance')) return '$n حضور';
    return '$n نقطة';
  }
}
