import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/billing_provider.dart';

class ActivePackagesScreen extends ConsumerWidget {
  const ActivePackagesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final packages = ref.watch(activePackagesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الباقات النشطة' : 'Active Packages'),
      ),
      body: packages.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.card_giftcard_outlined,
              title: isArabic ? 'لا توجد باقات نشطة' : 'No active packages',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(activePackagesProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final pkg = items[index];
                final progress = pkg.totalSessions > 0
                    ? pkg.usedSessions / pkg.totalSessions
                    : 0.0;
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              pkg.memberName ?? (isArabic ? 'عضو' : 'Member'),
                              style: theme.textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          Text(
                            pkg.packageName ?? '',
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: theme.colorScheme.primary,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          Text(
                            '${pkg.usedSessions}/${pkg.totalSessions} ${isArabic ? 'حصص' : 'sessions'}',
                            style: theme.textTheme.bodySmall,
                          ),
                          const Spacer(),
                          Text(
                            '${pkg.remainingSessions} ${isArabic ? 'متبقية' : 'remaining'}',
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: pkg.remainingSessions < 3
                                  ? AppColors.error
                                  : AppColors.success,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      LinearProgressIndicator(
                        value: progress.clamp(0.0, 1.0),
                        backgroundColor: theme.colorScheme.primary.withOpacity(0.1),
                        valueColor: AlwaysStoppedAnimation<Color>(
                          progress > 0.8 ? AppColors.error : theme.colorScheme.primary,
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
          message: isArabic ? 'خطأ في التحميل' : 'Error loading packages',
          onRetry: () => ref.invalidate(activePackagesProvider),
        ),
      ),
    );
  }
}
