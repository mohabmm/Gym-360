import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/billing_provider.dart';

/// Localizes the BE PaymentPurpose enum (Membership/SessionPackage/Product).
/// The revenue list previously printed the raw camelCase-spaced enum, leaking
/// English into the Arabic-first UI. Mirrors the record-payment chip labels.
String _purposeLabel(String purpose, bool isArabic) {
  switch (purpose.toLowerCase().replaceAll(RegExp(r'[\s_\-]'), '')) {
    case 'membership':
      return isArabic ? 'اشتراك' : 'Membership';
    case 'sessionpackage':
      return isArabic ? 'باقة حصص' : 'Session Package';
    case 'product':
      return isArabic ? 'منتج' : 'Product';
    default:
      return purpose;
  }
}

class RevenueDashboardScreen extends ConsumerWidget {
  const RevenueDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final payments = ref.watch(paymentsProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'لوحة الايرادات' : 'Revenue Dashboard'),
      ),
      body: payments.when(
        data: (items) {
          final total = items.fold<double>(0, (sum, p) => sum + p.amount);
          final completed = items.where((p) => p.status.toLowerCase() == 'completed').toList();
          final completedTotal = completed.fold<double>(0, (sum, p) => sum + p.amount);

          return RefreshIndicator(
            onRefresh: () => ref.refresh(paymentsProvider.future),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Row(
                  children: [
                    Expanded(
                      child: StatCard(
                        label: isArabic ? 'اجمالي المدفوعات' : 'Total Revenue',
                        value: NumberFormatter.formatCurrency(Egp(completedTotal)),
                        icon: Icons.attach_money,
                        iconColor: AppColors.success,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: StatCard(
                        label: isArabic ? 'عدد المعاملات' : 'Transactions',
                        value: '${items.length}',
                        icon: Icons.receipt_long,
                        iconColor: AppColors.info,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                Text(
                  isArabic ? 'المدفوعات الاخيرة' : 'Recent Payments',
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 12),
                ...items.take(20).map((p) => AppCard(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: _statusColor(p.status).withOpacity(0.1),
                            shape: BoxShape.circle,
                          ),
                          child: Icon(
                            Icons.payment,
                            color: _statusColor(p.status),
                            size: 20,
                          ),
                        ),
                        title: Text(
                          (p.memberName != null && p.memberName!.isNotEmpty)
                              ? p.memberName!
                              : NumberFormatter.formatCurrency(Egp(p.amount)),
                          style: theme.textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        subtitle: Text(
                          [
                            NumberFormatter.formatCurrency(Egp(p.amount)),
                            if (p.purpose.isNotEmpty)
                              _purposeLabel(p.purpose, isArabic),
                            if (p.createdAt != null)
                              DateFormatter.formatShort(p.createdAt!),
                          ].join(' · '),
                        ),
                        trailing: _StatusBadge(status: p.status, isArabic: isArabic),
                      ),
                    )),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل البيانات' : 'Error loading data',
          onRetry: () => ref.invalidate(paymentsProvider),
        ),
      ),
    );
  }

  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'completed':
        return AppColors.success;
      case 'voided':
        return AppColors.error;
      default:
        return AppColors.warning;
    }
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;
  final bool isArabic;

  const _StatusBadge({required this.status, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    Color color;
    String label;
    switch (status.toLowerCase()) {
      case 'completed':
        color = AppColors.success;
        label = isArabic ? 'مكتمل' : 'Completed';
        break;
      case 'voided':
        color = AppColors.error;
        label = isArabic ? 'ملغي' : 'Voided';
        break;
      default:
        color = AppColors.warning;
        label = isArabic ? 'معلق' : 'Pending';
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 11, color: color, fontWeight: FontWeight.w600),
      ),
    );
  }
}
