import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/settings_provider.dart';

class SessionPoliciesScreen extends ConsumerStatefulWidget {
  const SessionPoliciesScreen({super.key});

  @override
  ConsumerState<SessionPoliciesScreen> createState() =>
      _SessionPoliciesScreenState();
}

class _SessionPoliciesScreenState extends ConsumerState<SessionPoliciesScreen> {
  final _cancelWindowController = TextEditingController();
  final _noshowThresholdController = TextEditingController();
  final _noshowWindowDaysController = TextEditingController();
  String _noshowAction = 'FlagOnly';
  bool _saving = false;
  bool _initialized = false;

  static const _noshowActions = ['FlagOnly', 'NotifyTrainer', 'SuspendBooking'];

  @override
  void dispose() {
    _cancelWindowController.dispose();
    _noshowThresholdController.dispose();
    _noshowWindowDaysController.dispose();
    super.dispose();
  }

  void _initFrom(Map<String, dynamic> policies) {
    if (_initialized) return;
    _initialized = true;
    _cancelWindowController.text =
        (policies['cancellationWindowHours'] ?? 24).toString();
    _noshowThresholdController.text =
        (policies['noshowThreshold'] ?? 3).toString();
    _noshowWindowDaysController.text =
        (policies['noshowWindowDays'] ?? 30).toString();
    _noshowAction =
        (policies['noshowAction'] as String?) ?? 'FlagOnly';
  }

  Future<void> _save() async {
    final threshold = int.tryParse(_noshowThresholdController.text);
    final windowDays = int.tryParse(_noshowWindowDaysController.text);
    if (threshold == null || threshold <= 0 ||
        windowDays == null || windowDays <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            Localizations.localeOf(context).languageCode == 'ar'
                ? 'ادخل قيم صحيحة'
                : 'Enter valid values',
          ),
        ),
      );
      return;
    }

    setState(() => _saving = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);

      await apiClient.put<void>(
        ApiEndpoints.gymSessionPolicy(gymId),
        data: {
          'cancellationWindowHours':
              int.tryParse(_cancelWindowController.text) ?? 24,
          'noshowThreshold': threshold,
          'noshowWindowDays': windowDays,
          'noshowAction': _noshowAction,
        },
        parser: (_) {},
      );
      if (mounted) {
        ref.invalidate(gymSessionPoliciesProvider);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              Localizations.localeOf(context).languageCode == 'ar'
                  ? 'تم حفظ السياسات'
                  : 'Policies saved',
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final policies = ref.watch(gymSessionPoliciesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    final actionLabels = {
      'FlagOnly': isArabic ? 'تسجيل فقط' : 'Flag Only',
      'NotifyTrainer': isArabic ? 'اشعار المدرب' : 'Notify Trainer',
      'SuspendBooking': isArabic ? 'تعليق الحجز' : 'Suspend Booking',
    };

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'سياسات الحصص' : 'Session Policies'),
      ),
      body: policies.when(
        data: (p) {
          _initFrom(p);
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isArabic ? 'سياسة الالغاء' : 'Cancellation Policy',
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      isArabic
                          ? 'يمكن للاعضاء الغاء الحصة قبل: (Q5: قابل للضبط)'
                          : 'Members can cancel sessions within: (Q5: Configurable)',
                      style: theme.textTheme.bodySmall,
                    ),
                    const SizedBox(height: 12),
                    AppTextField(
                      controller: _cancelWindowController,
                      label: isArabic
                          ? 'نافذة الالغاء (ساعات)'
                          : 'Cancellation Window (hours)',
                      keyboardType: TextInputType.number,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isArabic ? 'سياسة الغياب' : 'No-Show Policy',
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 12),
                    AppTextField(
                      controller: _noshowThresholdController,
                      label: isArabic
                          ? 'عدد مرات الغياب قبل الاجراء'
                          : 'No-Show Count Before Action',
                      keyboardType: TextInputType.number,
                    ),
                    const SizedBox(height: 12),
                    AppTextField(
                      controller: _noshowWindowDaysController,
                      label: isArabic
                          ? 'فترة احتساب الغياب (ايام)'
                          : 'No-Show Window (days)',
                      keyboardType: TextInputType.number,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      isArabic ? 'اجراء الغياب' : 'No-Show Action',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 8,
                      children: _noshowActions.map((action) {
                        return ChoiceChip(
                          label: Text(actionLabels[action] ?? action),
                          selected: _noshowAction == action,
                          onSelected: (_) =>
                              setState(() => _noshowAction = action),
                        );
                      }).toList(),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),
              AppButton(
                text: isArabic ? 'حفظ السياسات' : 'Save Policies',
                isLoading: _saving,
                onPressed: _save,
              ),
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading policies',
          onRetry: () => ref.invalidate(gymSessionPoliciesProvider),
        ),
      ),
    );
  }
}
