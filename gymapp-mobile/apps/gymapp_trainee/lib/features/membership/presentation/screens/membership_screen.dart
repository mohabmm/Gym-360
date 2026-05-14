import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/membership_providers.dart';

class MembershipScreen extends ConsumerWidget {
  const MembershipScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final user = authState.valueOrNull?.user;
    final memberships = user?.gymMemberships ?? [];

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'عضويتي' : 'My Membership'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(activePackagesProvider);
          ref.invalidate(paymentHistoryProvider);
        },
        child: CustomScrollView(
          slivers: [
            // Current gym membership
            if (memberships.isNotEmpty)
              SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, index) => _MembershipCard(
                    membership: memberships[index],
                    isArabic: isArabic,
                  ),
                  childCount: memberships.length,
                ),
              ),

            // Session packages
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
                child: Text(
                  isArabic ? 'باقات الجلسات' : 'Session Packages',
                  style: Theme.of(context)
                      .textTheme
                      .titleSmall
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
              ),
            ),

            Consumer(
              builder: (context, ref, _) {
                final packagesAsync = ref.watch(activePackagesProvider);
                return packagesAsync.when(
                  data: (packages) {
                    if (packages.isEmpty) {
                      return SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 16, vertical: 8),
                          child: Text(
                            isArabic
                                ? 'لا توجد باقات نشطة'
                                : 'No active packages',
                            style:
                                Theme.of(context).textTheme.bodyMedium,
                          ),
                        ),
                      );
                    }
                    return SliverList(
                      delegate: SliverChildBuilderDelegate(
                        (ctx, i) => _PackageTile(
                          package: packages[i],
                          isArabic: isArabic,
                        ),
                        childCount: packages.length,
                      ),
                    );
                  },
                  loading: () => const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(16),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  ),
                  error: (e, _) => const SliverToBoxAdapter(
                      child: SizedBox.shrink()),
                );
              },
            ),

            // Payment history
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
                child: Text(
                  isArabic ? 'سجل المدفوعات' : 'Payment History',
                  style: Theme.of(context)
                      .textTheme
                      .titleSmall
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
              ),
            ),

            Consumer(
              builder: (context, ref, _) {
                final paymentsAsync = ref.watch(paymentHistoryProvider);
                return paymentsAsync.when(
                  data: (payments) {
                    if (payments.isEmpty) {
                      return SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 16, vertical: 8),
                          child: Text(
                            isArabic
                                ? 'لا توجد مدفوعات'
                                : 'No payments',
                            style:
                                Theme.of(context).textTheme.bodyMedium,
                          ),
                        ),
                      );
                    }
                    return SliverList(
                      delegate: SliverChildBuilderDelegate(
                        (ctx, i) => _PaymentTile(
                          payment: payments[i],
                          isArabic: isArabic,
                        ),
                        childCount: payments.length,
                      ),
                    );
                  },
                  loading: () => const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(16),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  ),
                  error: (e, _) => const SliverToBoxAdapter(
                      child: SizedBox.shrink()),
                );
              },
            ),

            const SliverToBoxAdapter(child: SizedBox(height: 24)),
          ],
        ),
      ),
    );
  }
}

class _MembershipCard extends StatelessWidget {
  final GymMembershipModel membership;
  final bool isArabic;

  const _MembershipCard(
      {required this.membership, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    // /identity/profile returns only isActive flag for membership status;
    // membership.status (string) and membershipType are optional extras. Use
    // isActive as the authoritative "active" signal and hide the badge
    // entirely when we have nothing meaningful to show (fixes BUG-12 where
    // the card rendered with two empty rows).
    final active = membership.isActive ?? (membership.status == 'active');
    final statusText = active
        ? (isArabic ? 'نشط' : 'Active')
        : (membership.status != null && membership.status!.isNotEmpty
            ? membership.status!
            : (isArabic ? 'غير نشط' : 'Inactive'));
    final statusColor = active
        ? Colors.green
        : (membership.status == 'frozen' ? Colors.blue : Colors.red);

    final subtitle = membership.membershipType ??
        (membership.section != null
            ? (isArabic ? 'قسم: ${membership.section}' : 'Section: ${membership.section}')
            : null);

    return AppCard(
      margin: const EdgeInsets.fromLTRB(16, 8, 16, 4),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    membership.gymName ?? (isArabic ? 'الصالة' : 'Gym'),
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusColor.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    statusText,
                    style: TextStyle(
                        color: statusColor,
                        fontWeight: FontWeight.w600,
                        fontSize: 12),
                  ),
                ),
              ],
            ),
            if (membership.memberNumber != null &&
                membership.memberNumber!.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                '${isArabic ? 'رقم العضوية:' : 'Member #'} ${membership.memberNumber}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
            if (subtitle != null) ...[
              const SizedBox(height: 8),
              Text(
                subtitle,
                style: Theme.of(context)
                    .textTheme
                    .bodyMedium
                    ?.copyWith(
                        color: Theme.of(context).colorScheme.primary),
              ),
            ],
            if (membership.endDate != null) ...[
              const SizedBox(height: 4),
              Row(
                children: [
                  const Icon(Icons.event_outlined, size: 14),
                  const SizedBox(width: 4),
                  Text(
                    '${isArabic ? 'تنتهي في:' : 'Expires:'} ${DateFormat(isArabic ? 'dd/MM/yyyy' : 'MMM dd, yyyy').format(membership.endDate!)}',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ],
            if (membership.assignedTrainerName != null) ...[
              const SizedBox(height: 4),
              Row(
                children: [
                  const Icon(Icons.person_outline, size: 14),
                  const SizedBox(width: 4),
                  Text(
                    '${isArabic ? 'المدرب:' : 'Trainer:'} ${membership.assignedTrainerName}',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _PackageTile extends StatelessWidget {
  final SessionPackageModel package;
  final bool isArabic;

  const _PackageTile({required this.package, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final pct = package.totalSessions > 0
        ? package.remainingSessions / package.totalSessions
        : 0.0;

    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              package.packageName ??
                  (isArabic ? 'باقة جلسات' : 'Session Package'),
              style:
                  const TextStyle(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                Expanded(
                  child: LinearProgressIndicator(
                    value: pct.clamp(0.0, 1.0),
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
                const SizedBox(width: 12),
                Text(
                  '${package.remainingSessions}/${package.totalSessions}',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
            if (package.expiryDate != null) ...[
              const SizedBox(height: 4),
              Text(
                '${isArabic ? 'تنتهي:' : 'Expires:'} ${DateFormat('dd/MM/yyyy').format(package.expiryDate!)}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _PaymentTile extends StatelessWidget {
  final PaymentModel payment;
  final bool isArabic;

  const _PaymentTile({required this.payment, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final dateStr = payment.paidAt != null
        ? DateFormat(isArabic ? 'dd/MM/yyyy' : 'MMM dd, yyyy')
            .format(payment.paidAt!)
        : '-';

    final method = payment.method;
    final hasMethod = method.trim().isNotEmpty && method.toLowerCase() != 'none';
    final subtitleText = hasMethod ? '$dateStr • ${_methodLabel(method, isArabic)}' : dateStr;

    return ListTile(
      leading: Icon(
        Icons.receipt_outlined,
        color: payment.status == 'voided' ? Colors.red : Colors.green,
      ),
      title: Text(_purposeLabel(payment.purpose, isArabic)),
      subtitle: Text(subtitleText),
      trailing: Text(
        '${payment.amount.toStringAsFixed(0)} ${payment.currency}',
        style: TextStyle(
          color:
              payment.status == 'voided' ? Colors.red : Colors.green,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

/// Turns the backend's bare enum name for a payment purpose
/// (Membership, SessionPackage, DropIn, Other) into a user-friendly label.
String _purposeLabel(String purpose, bool isArabic) {
  switch (purpose) {
    case 'Membership':
      return isArabic ? 'اشتراك العضوية' : 'Membership';
    case 'SessionPackage':
      return isArabic ? 'باقة جلسات' : 'Session Package';
    case 'DropIn':
    case 'SingleSession':
      return isArabic ? 'جلسة مفردة' : 'Single Session';
    case 'Other':
      return isArabic ? 'أخرى' : 'Other';
    default:
      return purpose;
  }
}

String _methodLabel(String method, bool isArabic) {
  switch (method) {
    case 'Cash':
      return isArabic ? 'نقداً' : 'Cash';
    case 'Card':
      return isArabic ? 'بطاقة' : 'Card';
    case 'BankTransfer':
      return isArabic ? 'تحويل بنكي' : 'Bank transfer';
    case 'Wallet':
      return isArabic ? 'محفظة إلكترونية' : 'Wallet';
    default:
      return method;
  }
}
