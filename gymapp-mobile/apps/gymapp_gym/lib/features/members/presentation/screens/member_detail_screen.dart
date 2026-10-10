import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/error_text.dart';
import '../../../../core/providers.dart';
import '../../../trainers/presentation/providers/trainers_provider.dart';
import '../providers/members_provider.dart';

const List<int> _kSessionCountOptions = [4, 8, 12, 16, 20, 24, 28, 32, 36, 40];

class MemberDetailScreen extends ConsumerWidget {
  final String memberId;

  const MemberDetailScreen({super.key, required this.memberId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final member = ref.watch(memberDetailProvider(memberId));
    final payments = ref.watch(memberPaymentsProvider(memberId));
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider).valueOrNull;
    final canFreeze = authState?.hasPermission(StaffPermission.freezeMember) ?? false;
    final canDelete = authState?.hasPermission(StaffPermission.deleteMember) ?? false;
    final canAssignTrainer = authState?.hasPermission(StaffPermission.assignPackage) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ملف العضو' : 'Member Profile'),
      ),
      body: member.when(
        data: (m) {
          final membership = m.gymMemberships?.isNotEmpty == true
              ? m.gymMemberships!.first
              : null;
          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(memberDetailProvider(memberId)),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // Profile Header
                AppCard(
                  child: Row(
                    children: [
                      AvatarWidget(
                        imageUrl: m.photoUrl,
                        name: '${m.firstName} ${m.lastName}',
                        size: 64,
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              '${m.firstName} ${m.lastName}',
                              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                                    fontWeight: FontWeight.w700,
                                  ),
                            ),
                            if (m.status?.toLowerCase() == AppConstants.memberFrozen)
                              Padding(
                                padding: const EdgeInsets.symmetric(vertical: 4),
                                child: Chip(
                                  avatar: const Icon(Icons.ac_unit, size: 16),
                                  label: Text(isArabic ? 'مجمد' : 'Frozen'),
                                  visualDensity: VisualDensity.compact,
                                ),
                              ),
                            if (m.memberNumber != null)
                              Text(
                                '#${m.memberNumber}',
                                style: Theme.of(context).textTheme.bodySmall,
                              ),
                            if (m.phone != null) Text(m.phone!),
                            if (m.email.isNotEmpty) Text(m.email),
                            if (m.nationalId != null && m.nationalId!.isNotEmpty)
                              Text(
                                '${isArabic ? 'الرقم القومي' : 'National ID'}: ${m.nationalId!}',
                                style: Theme.of(context).textTheme.bodySmall,
                              ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),

                // Membership Details
                if (membership != null)
                  AppCard(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isArabic ? 'تفاصيل الاشتراك' : 'Membership Details',
                          style: Theme.of(context).textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                        ),
                        const SizedBox(height: 8),
                        _DetailRow(
                          label: isArabic ? 'النوع' : 'Type',
                          value: membership.membershipType ?? '-',
                        ),
                        _DetailRow(
                          label: isArabic ? 'تاريخ الانتهاء' : 'Expires',
                          value: membership.endDate != null
                              ? DateFormatter.formatShort(membership.endDate!)
                              : '-',
                        ),
                        _DetailRow(
                          label: isArabic ? 'الحالة' : 'Status',
                          value: membership.status ?? '-',
                        ),
                      ],
                    ),
                  ),
                const SizedBox(height: 12),

                // Quick Actions
                if (canAssignTrainer)
                  SizedBox(
                    width: double.infinity,
                    child: AppButton(
                      text: isArabic ? 'تعيين مدرب' : 'Assign Trainer',
                      icon: Icons.fitness_center,
                      onPressed: () => _showAssignTrainerDialog(context, ref, isArabic),
                    ),
                  ),
                if (canAssignTrainer) const SizedBox(height: 8),
                if (canFreeze)
                  SizedBox(
                    width: double.infinity,
                    child: m.status?.toLowerCase() == AppConstants.memberFrozen
                        ? AppButton.outlined(
                            text: isArabic ? 'الغاء التجميد' : 'Unfreeze',
                            icon: Icons.play_arrow,
                            onPressed: () => _showFreezeDialog(
                                context, ref, isArabic,
                                freeze: false),
                          )
                        : AppButton.outlined(
                            text: isArabic ? 'تجميد' : 'Freeze',
                            icon: Icons.ac_unit,
                            onPressed: () => _showFreezeDialog(
                                context, ref, isArabic,
                                freeze: true),
                          ),
                  ),
                const SizedBox(height: 20),

                // Payment History
                Text(
                  isArabic ? 'سجل المدفوعات' : 'Payment History',
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                ),
                const SizedBox(height: 8),
                payments.when(
                  data: (payList) {
                    if (payList.isEmpty) {
                      return Text(
                        isArabic ? 'لا توجد مدفوعات' : 'No payments yet',
                        style: Theme.of(context).textTheme.bodyMedium,
                      );
                    }
                    return Column(
                      children: payList.map((p) {
                        return ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: const Icon(Icons.receipt),
                          title: Text(
                            NumberFormatter.formatCurrency(p.amount),
                          ),
                          subtitle: p.createdAt != null
                              ? Text(DateFormatter.formatShort(p.createdAt!))
                              : null,
                          trailing: Text(_paymentStatusLabel(p.status, isArabic)),
                        );
                      }).toList(),
                    );
                  },
                  loading: () => const CircularProgressIndicator(),
                  error: (_, __) => Text(isArabic ? 'تعذر التحميل' : 'Failed to load'),
                ),

                if (canDelete) ...[
                  const SizedBox(height: 20),
                  AppButton(
                    text: isArabic ? 'حذف العضو' : 'Remove Member',
                    backgroundColor: AppColors.error,
                    onPressed: () => _showDeleteDialog(context, ref, isArabic),
                  ),
                ],
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل الملف' : 'Error loading profile',
          onRetry: () => ref.invalidate(memberDetailProvider(memberId)),
        ),
      ),
    );
  }

  Future<void> _showAssignTrainerDialog(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final trainersAsync = await ref.read(trainersProvider.future).catchError((_) => <UserModel>[]);
    final eligible = trainersAsync.where((t) {
      final s = (t.status ?? '').toLowerCase();
      return s != 'pending_approval' && s != 'rejected';
    }).toList();

    if (!context.mounted) return;
    if (eligible.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic ? 'لا يوجد مدربون مؤهلون' : 'No approved trainers at this gym yet.'),
      ));
      return;
    }

    String? selectedTrainerId;
    int selectedSessions = _kSessionCountOptions.first;
    final priceController = TextEditingController();
    String selectedMethod = 'Cash';

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(isArabic ? 'تعيين مدرب' : 'Assign Trainer'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                DropdownButtonFormField<String>(
                  value: selectedTrainerId,
                  isExpanded: true,
                  hint: Text(isArabic ? 'اختر المدرب' : 'Pick a trainer'),
                  items: eligible
                      .map((t) => DropdownMenuItem(
                            value: t.id,
                            child: Text('${t.firstName} ${t.lastName}'.trim()),
                          ))
                      .toList(),
                  onChanged: (v) => setState(() => selectedTrainerId = v),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  value: selectedSessions,
                  isExpanded: true,
                  items: _kSessionCountOptions
                      .map((n) => DropdownMenuItem(
                            value: n,
                            child: Text(isArabic ? '$n حصة' : '$n sessions'),
                          ))
                      .toList(),
                  onChanged: (v) => setState(() => selectedSessions = v ?? _kSessionCountOptions.first),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: priceController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: InputDecoration(
                    labelText: isArabic ? 'السعر الإجمالي (ج.م)' : 'Total price (EGP)',
                    hintText: isArabic ? 'اتركه فارغًا لمجاني' : 'Leave blank for free',
                  ),
                  onChanged: (_) => setState(() {}),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  value: selectedMethod,
                  isExpanded: true,
                  decoration: InputDecoration(labelText: isArabic ? 'طريقة الدفع' : 'Payment method'),
                  items: [
                    DropdownMenuItem(
                        value: 'Cash',
                        child: Text(isArabic ? 'نقدي' : 'Cash')),
                    DropdownMenuItem(
                        value: 'Card',
                        child: Text(isArabic ? 'بطاقة' : 'Card')),
                    DropdownMenuItem(
                        value: 'BankTransfer',
                        child: Text(isArabic ? 'تحويل بنكي' : 'Bank Transfer')),
                    DropdownMenuItem(
                        value: 'MobileWallet',
                        child: Text(isArabic ? 'محفظة الكترونية' : 'Mobile Wallet')),
                  ],
                  onChanged: (v) => setState(() => selectedMethod = v ?? 'Cash'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: Text(isArabic ? 'الغاء' : 'Cancel'),
            ),
            TextButton(
              onPressed: selectedTrainerId == null ? null : () => Navigator.of(ctx).pop(true),
              child: Text(isArabic ? 'تعيين' : 'Assign'),
            ),
          ],
        ),
      ),
    );
    if (confirmed != true || selectedTrainerId == null) return;

    final priceText = priceController.text.trim();
    final priceNum = priceText.isEmpty ? null : double.tryParse(priceText);
    if (priceText.isNotEmpty && (priceNum == null || priceNum < 0)) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic ? 'سعر غير صالح' : 'Invalid price.'),
      ));
      return;
    }

    final messenger = ScaffoldMessenger.of(context);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      final assignedBy = ref.read(authProvider).valueOrNull?.user?.id;
      if (assignedBy == null) throw const ApiException(message: 'No active session.', errorCode: 'NO_SESSION');
      await apiClient.post<void>(
        ApiEndpoints.quickAssignPackage,
        data: {
          'gymId': gymId,
          'trainerId': selectedTrainerId,
          'memberId': memberId,
          'sessionCount': selectedSessions,
          'assignedBy': assignedBy,
          if (priceNum != null && priceNum > 0) 'totalPrice': priceNum,
          if (priceNum != null && priceNum > 0) 'paymentMethod': selectedMethod,
        },
        parser: (_) {},
      );
      ref.invalidate(memberDetailProvider(memberId));
      ref.invalidate(membersProvider);
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic ? 'تم التعيين' : 'Trainer assigned'),
      ));
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(staffErrorText(e, isArabic: isArabic))));
    }
  }

  Future<void> _showFreezeDialog(
    BuildContext context,
    WidgetRef ref,
    bool isArabic, {
    required bool freeze,
  }) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(freeze
            ? (isArabic ? 'تجميد العضوية' : 'Freeze Membership')
            : (isArabic ? 'الغاء تجميد العضوية' : 'Unfreeze Membership')),
        content: Text(
          freeze
              ? (isArabic
                  ? 'هل تريد تجميد عضوية هذا العضو؟'
                  : 'Are you sure you want to freeze this membership?')
              : (isArabic
                  ? 'هل تريد اعادة تفعيل عضوية هذا العضو؟'
                  : 'Reactivate this membership?'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: Text(freeze
                ? (isArabic ? 'تجميد' : 'Freeze')
                : (isArabic ? 'الغاء التجميد' : 'Unfreeze')),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    final messenger = ScaffoldMessenger.of(context);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.post<void>(
        freeze
            ? ApiEndpoints.gymMemberFreeze(gymId, memberId)
            : ApiEndpoints.gymMemberUnfreeze(gymId, memberId),
        parser: (_) {},
      );
      ref.invalidate(memberDetailProvider(memberId));
      ref.invalidate(membersProvider);
      messenger.showSnackBar(
        SnackBar(
          content: Text(freeze
              ? (isArabic ? 'تم التجميد' : 'Membership frozen')
              : (isArabic ? 'تم الغاء التجميد' : 'Membership reactivated')),
        ),
      );
    } catch (e) {
      messenger.showSnackBar(
          SnackBar(content: Text(staffErrorText(e, isArabic: isArabic))));
    }
  }

  Future<void> _showDeleteDialog(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'حذف العضو' : 'Remove Member'),
        content: Text(
          isArabic
              ? 'هل انت متاكد من حذف هذا العضو؟ لا يمكن التراجع عن هذا الاجراء.'
              : 'Are you sure you want to remove this member? This action cannot be undone.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            child: Text(isArabic ? 'حذف' : 'Remove'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    final messenger = ScaffoldMessenger.of(context);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.delete(ApiEndpoints.gymMemberById(gymId, memberId));
      ref.invalidate(membersProvider);
      messenger.showSnackBar(
        SnackBar(
          content: Text(isArabic ? 'تم حذف العضو' : 'Member removed'),
        ),
      );
      if (context.mounted) context.pop();
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(staffErrorText(e, isArabic: isArabic))));
    }
  }
}

String _paymentStatusLabel(String status, bool isArabic) {
  if (!isArabic) return status;
  switch (status.toLowerCase()) {
    case 'completed':
    case 'succeeded':
    case 'paid':
      return 'مكتمل';
    case 'pending':
      return 'قيد الانتظار';
    case 'failed':
      return 'فشل';
    case 'refunded':
      return 'مسترد';
    case 'voided':
    case 'void':
      return 'ملغي';
    default:
      return status;
  }
}

class _DetailRow extends StatelessWidget {
  final String label;
  final String value;

  const _DetailRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Text(label, style: Theme.of(context).textTheme.bodySmall),
          const Spacer(),
          Text(
            value,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.w500,
                ),
          ),
        ],
      ),
    );
  }
}
