import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/billing_provider.dart';
import '../../../members/presentation/providers/members_provider.dart';

class RecordPaymentScreen extends ConsumerStatefulWidget {
  const RecordPaymentScreen({super.key});

  @override
  ConsumerState<RecordPaymentScreen> createState() =>
      _RecordPaymentScreenState();
}

class _RecordPaymentScreenState extends ConsumerState<RecordPaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _memberIdController = TextEditingController();
  final _amountController = TextEditingController();
  String _method = 'Cash';
  String _purpose = 'Membership';
  bool _loading = false;

  @override
  void dispose() {
    _memberIdController.dispose();
    _amountController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);

      final memberInput = _memberIdController.text.trim();
      final pagedMembers = await ref.read(membersProvider.future);
      final match = pagedMembers.items.firstWhere(
        (m) =>
            (m.memberNumber?.toLowerCase() == memberInput.toLowerCase()) ||
            m.id == memberInput,
        orElse: () => throw Exception(
          Localizations.localeOf(context).languageCode == 'ar'
              ? 'العضو غير موجود'
              : 'Member not found',
        ),
      );

      await apiClient.post<void>(
        ApiEndpoints.payments,
        data: {
          'gymId': gymId,
          'memberId': match.id,
          'amount': double.tryParse(_amountController.text) ?? 0,
          'paymentMethod': _method,
          'purpose': _purpose,
        },
        parser: (_) {},
      );
      if (mounted) {
        ref.invalidate(paymentsProvider);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              Localizations.localeOf(context).languageCode == 'ar'
                  ? 'تم تسجيل الدفع'
                  : 'Payment recorded',
            ),
          ),
        );
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e is ApiException ? e.message : e.toString())));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تسجيل دفع' : 'Record Payment'),
      ),
      body: Column(
        children: [
          Expanded(
            child: Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  AppTextField(
                    controller: _memberIdController,
                    label: isArabic ? 'رقم العضو' : 'Member Number / ID',
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _amountController,
                    label: isArabic ? 'المبلغ (ج.م)' : 'Amount (EGP)',
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      if (v == null || v.isEmpty) return isArabic ? 'مطلوب' : 'Required';
                      final amount = double.tryParse(v);
                      if (amount == null) return isArabic ? 'رقم غير صالح' : 'Invalid amount';
                      if (amount <= 0) {
                        return isArabic
                            ? 'يجب أن يكون المبلغ أكبر من صفر'
                            : 'Amount must be greater than zero';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  Text(isArabic ? 'طريقة الدفع' : 'Payment Method',
                      style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w500)),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: ['Cash', 'Card', 'BankTransfer'].map((m) {
                      final labels = {
                        'Cash': isArabic ? 'نقدي' : 'Cash',
                        'Card': isArabic ? 'بطاقة' : 'Card',
                        'BankTransfer': isArabic ? 'تحويل بنكي' : 'Bank Transfer',
                      };
                      return ChoiceChip(
                        label: Text(labels[m] ?? m),
                        selected: _method == m,
                        onSelected: (_) => setState(() => _method = m),
                      );
                    }).toList(),
                  ),
                  const SizedBox(height: 16),
                  Text(isArabic ? 'نوع الدفع' : 'Payment Type',
                      style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w500)),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: ['Membership', 'SessionPackage', 'Product'].map((t) {
                      final labels = {
                        'Membership': isArabic ? 'اشتراك' : 'Membership',
                        'SessionPackage': isArabic ? 'باقة حصص' : 'Session Package',
                        'Product': isArabic ? 'منتج' : 'Product',
                      };
                      return ChoiceChip(
                        label: Text(labels[t] ?? t),
                        selected: _purpose == t,
                        onSelected: (_) => setState(() => _purpose = t),
                      );
                    }).toList(),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'تسجيل الدفع' : 'Record Payment',
              isLoading: _loading,
              onPressed: _submit,
            ),
          ),
        ],
      ),
    );
  }
}
