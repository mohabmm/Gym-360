import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../clients/presentation/providers/client_providers.dart';
import '../providers/revenue_providers.dart';

class ClientPackagesScreen extends ConsumerWidget {
  const ClientPackagesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final packagesAsync = ref.watch(allClientPackagesProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'باقات العملاء' : 'Client Packages'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(allClientPackagesProvider),
        child: packagesAsync.when(
          data: (packages) {
            if (packages.isEmpty) {
              return EmptyState(
                icon: Icons.inventory_2_outlined,
                title: isArabic ? 'لا توجد باقات' : 'No packages',
                subtitle: isArabic
                    ? 'باقات جلسات عملائك ستظهر هنا'
                    : "Your clients' session packages will appear here",
              );
            }

            final clientNames = ref.watch(clientNameByIdProvider);
            return ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: packages.length,
              itemBuilder: (context, index) {
                final pkg = packages[index];
                final isLow = pkg.remainingSessions <= 3;
                final progressPercent = pkg.totalSessions > 0
                    ? pkg.remainingSessions / pkg.totalSessions
                    : 0.0;
                final displayName = pkg.packageName ?? pkg.name;
                final displayExpiry = pkg.expiryDate ?? pkg.expiresAt;
                // Prefer the BE-provided memberName when present; fall back
                // to the trainer's roster lookup (my-clients returns memberId
                // but no name on the flat SessionPackageDto).
                final memberDisplay = (pkg.memberName != null &&
                        pkg.memberName!.isNotEmpty)
                    ? pkg.memberName!
                    : clientNames[pkg.memberId];

                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (memberDisplay != null &&
                            memberDisplay.isNotEmpty) ...[
                          Row(
                            children: [
                              AvatarWidget(
                                name: memberDisplay,
                                size: AppConstants.avatarSizeSmall,
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  memberDisplay,
                                  style: theme.textTheme.titleMedium
                                      ?.copyWith(
                                          fontWeight: FontWeight.w700),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),
                        ],
                        Row(
                          mainAxisAlignment:
                              MainAxisAlignment.spaceBetween,
                          children: [
                            Expanded(
                              child: Text(
                                displayName ??
                                    (isArabic ? 'باقة' : 'Package'),
                                style: theme.textTheme.titleSmall
                                    ?.copyWith(
                                        fontWeight: FontWeight.w600),
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 8, vertical: 4),
                              decoration: BoxDecoration(
                                color: _statusColor(pkg.status)
                                    .withOpacity(0.1),
                                borderRadius:
                                    BorderRadius.circular(12),
                              ),
                              child: Text(
                                _statusLabel(pkg.status, isArabic),
                                style: TextStyle(
                                  color: _statusColor(pkg.status),
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Row(
                          mainAxisAlignment:
                              MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              '${pkg.remainingSessions} / ${pkg.totalSessions} ${isArabic ? 'جلسات' : 'sessions'}',
                              style: theme.textTheme.bodySmall,
                            ),
                            if (isLow)
                              Text(
                                isArabic ? 'منخفض!' : 'Low!',
                                style: TextStyle(
                                  color: AppColors.warning,
                                  fontSize: 12,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        LinearProgressIndicator(
                          value: progressPercent.clamp(0.0, 1.0),
                          backgroundColor: AppColors.grey200,
                          color: isLow
                              ? AppColors.warning
                              : AppColors.success,
                        ),
                        if (displayExpiry != null) ...[
                          const SizedBox(height: 4),
                          Text(
                            '${isArabic ? 'ينتهي' : 'Expires'}: ${DateFormat('dd/MM/yyyy').format(displayExpiry)}',
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: AppColors.grey400,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                );
              },
            );
          },
          loading: () => ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: 4,
            itemBuilder: (_, __) => const Padding(
              padding: EdgeInsets.only(bottom: 12),
              child: ShimmerCard(height: 110),
            ),
          ),
          error: (e, _) => AppErrorWidget(
            message: e.toString(),
            onRetry: () => ref.invalidate(allClientPackagesProvider),
          ),
        ),
      ),
    );
  }

  Color _statusColor(String? status) {
    switch (status?.toLowerCase()) {
      case 'active':
        return AppColors.success;
      case 'expired':
        return AppColors.error;
      case 'depleted':
        return AppColors.grey500;
      default:
        return AppColors.info;
    }
  }

  String _statusLabel(String? status, bool isArabic) {
    switch (status?.toLowerCase()) {
      case 'active':
        return isArabic ? 'نشط' : 'Active';
      case 'expired':
        return isArabic ? 'منتهي' : 'Expired';
      case 'depleted':
        return isArabic ? 'مستنفذ' : 'Depleted';
      default:
        return status ?? (isArabic ? 'غير معروف' : 'Unknown');
    }
  }
}
