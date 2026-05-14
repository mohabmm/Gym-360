import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/revenue_providers.dart';

class EarningsDashboardScreen extends ConsumerWidget {
  const EarningsDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final earningsAsync = ref.watch(trainerEarningsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الأرباح' : 'Earnings'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(trainerEarningsProvider),
        child: earningsAsync.when(
          data: (summary) {
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // Read-only notice
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppColors.info.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.info_outline,
                          color: AppColors.info, size: 16),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          isArabic
                              ? 'هذه البيانات للعرض فقط. يتم احتساب الأرباح من قِبل إدارة الصالة.'
                              : 'Read-only. Earnings are calculated by gym management.',
                          style: theme.textTheme.bodySmall,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),

                // Stats grid — sessions this month
                Row(
                  children: [
                    Expanded(
                      child: StatCard(
                        label: isArabic ? 'جلسات هذا الشهر' : 'Sessions This Month',
                        value: '${summary.totalSessions}',
                        icon: Icons.event,
                        iconColor: AppColors.info,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: StatCard(
                        label: isArabic ? 'إجمالي هذا الشهر' : 'Total This Month',
                        value:
                            '${summary.totalEarningsEgp.toStringAsFixed(0)} ${isArabic ? 'ج.م' : 'EGP'}',
                        icon: Icons.account_balance_wallet_outlined,
                        iconColor: AppColors.success,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // Per-session breakdown
                if (summary.records.isNotEmpty) ...[
                  Text(
                    isArabic ? 'تفاصيل الجلسات' : 'Session Breakdown',
                    style: theme.textTheme.titleSmall
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 8),
                  ...summary.records.map(
                    (record) => Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        dense: true,
                        title: Text(
                          isArabic ? 'جلسة مكتملة' : 'Completed Session',
                          style: theme.textTheme.bodyMedium
                              ?.copyWith(fontWeight: FontWeight.w600),
                        ),
                        subtitle: Text(
                          _formatDate(record.earnedAt, isArabic),
                          style: theme.textTheme.bodySmall,
                        ),
                        trailing: Text(
                          '${record.rateApplied.toStringAsFixed(0)} ${isArabic ? 'ج.م' : 'EGP'}',
                          style: theme.textTheme.bodyMedium?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                      ),
                    ),
                  ),
                ] else ...[
                  const SizedBox(height: 8),
                  Text(
                    isArabic
                        ? 'لا توجد جلسات مكتملة هذا الشهر.'
                        : 'No completed sessions this month.',
                    style: theme.textTheme.bodySmall
                        ?.copyWith(color: AppColors.grey500),
                  ),
                ],
              ],
            );
          },
          loading: () => ListView(
            padding: const EdgeInsets.all(16),
            children: const [
              ShimmerCard(height: 60),
              SizedBox(height: 12),
              ShimmerCard(height: 100),
              SizedBox(height: 12),
              ShimmerCard(height: 150),
            ],
          ),
          error: (e, _) => AppErrorWidget(
            message: e.toString(),
            onRetry: () => ref.invalidate(trainerEarningsProvider),
          ),
        ),
      ),
    );
  }

  String _formatDate(DateTime dt, bool isArabic) {
    final day = dt.day.toString().padLeft(2, '0');
    final month = dt.month.toString().padLeft(2, '0');
    final year = dt.year;
    return isArabic ? '$day/$month/$year' : '$month/$day/$year';
  }
}
