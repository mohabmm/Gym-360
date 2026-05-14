import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/settings_provider.dart';

class KioskPinScreen extends ConsumerStatefulWidget {
  const KioskPinScreen({super.key});

  @override
  ConsumerState<KioskPinScreen> createState() => _KioskPinScreenState();
}

class _KioskPinScreenState extends ConsumerState<KioskPinScreen> {
  final _currentCtrl = TextEditingController();
  final _newCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  bool _saving = false;
  String? _error;

  @override
  void dispose() {
    _currentCtrl.dispose();
    _newCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  bool _isPinShape(String v) =>
      RegExp(r'^\d{4,8}$').hasMatch(v);

  Future<void> _save({required bool isSet, required bool isArabic}) async {
    setState(() => _error = null);

    final newPin = _newCtrl.text.trim();
    final confirmPin = _confirmCtrl.text.trim();
    final currentPin = _currentCtrl.text.trim();

    if (!_isPinShape(newPin)) {
      setState(() => _error = isArabic
          ? 'يجب ان يكون PIN بين 4 و 8 ارقام'
          : 'PIN must be 4–8 digits');
      return;
    }
    if (newPin != confirmPin) {
      setState(() => _error = isArabic
          ? 'تأكيد PIN غير مطابق'
          : 'New PIN and confirmation do not match');
      return;
    }
    if (isSet && !_isPinShape(currentPin)) {
      setState(() => _error = isArabic
          ? 'ادخل PIN الحالي'
          : 'Enter the current PIN');
      return;
    }

    setState(() => _saving = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.putVoid(
        ApiEndpoints.kioskPin(gymId),
        data: {
          'newPin': newPin,
          if (isSet) 'currentPin': currentPin,
        },
      );
      ref.invalidate(kioskPinInfoProvider);
      _currentCtrl.clear();
      _newCtrl.clear();
      _confirmCtrl.clear();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(isArabic ? 'تم حفظ PIN' : 'PIN saved')),
      );
    } on ApiException catch (e) {
      final msg = e is ForbiddenException
          ? (isArabic
              ? 'ليس لديك صلاحية لتغيير PIN الكيوسك'
              : 'You do not have permission to change the Kiosk PIN')
          : e.message;
      setState(() => _error = msg);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _remove({required bool isArabic}) async {
    final currentPin = _currentCtrl.text.trim();
    if (!_isPinShape(currentPin)) {
      setState(() => _error = isArabic
          ? 'ادخل PIN الحالي للإزالة'
          : 'Enter the current PIN to remove it');
      return;
    }
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'ازالة PIN' : 'Remove PIN'),
        content: Text(isArabic
            ? 'سيتم الخروج من وضع الكيوسك بدون PIN. هل انت متأكد؟'
            : 'Kiosk mode will exit without a PIN. Are you sure?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: Text(
              isArabic ? 'ازالة' : 'Remove',
              style: const TextStyle(color: AppColors.error),
            ),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.delete(
        ApiEndpoints.kioskPin(gymId),
        data: {'currentPin': currentPin},
      );
      ref.invalidate(kioskPinInfoProvider);
      _currentCtrl.clear();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(isArabic ? 'تمت ازالة PIN' : 'PIN removed')),
      );
    } on ApiException catch (e) {
      final msg = e is ForbiddenException
          ? (isArabic
              ? 'ليس لديك صلاحية لإزالة PIN الكيوسك'
              : 'You do not have permission to remove the Kiosk PIN')
          : e.message;
      setState(() => _error = msg);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final info = ref.watch(kioskPinInfoProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'رقم PIN الكيوسك' : 'Kiosk PIN'),
      ),
      body: info.when(
        data: (i) {
          final isSet = i.isSet;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isSet
                          ? (isArabic
                              ? 'PIN مضبوط. سيطلب عند الخروج من الكيوسك.'
                              : 'PIN is set. It will be required to exit kiosk mode.')
                          : (isArabic
                              ? 'لا يوجد PIN. سيتم الخروج من الكيوسك بدون سؤال. نوصي بضبط PIN لحماية الجهاز.'
                              : 'No PIN is set. Kiosk mode will exit without prompting. Set a PIN to protect the device.'),
                      style: Theme.of(context).textTheme.bodyMedium,
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
                      isSet
                          ? (isArabic ? 'تغيير PIN' : 'Change PIN')
                          : (isArabic ? 'ضبط PIN' : 'Set PIN'),
                      style: Theme.of(context)
                          .textTheme
                          .titleMedium
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 12),
                    if (isSet) ...[
                      _PinField(
                        controller: _currentCtrl,
                        label: isArabic ? 'PIN الحالي' : 'Current PIN',
                      ),
                      const SizedBox(height: 12),
                    ],
                    _PinField(
                      controller: _newCtrl,
                      label: isArabic ? 'PIN جديد' : 'New PIN',
                    ),
                    const SizedBox(height: 12),
                    _PinField(
                      controller: _confirmCtrl,
                      label: isArabic ? 'تأكيد PIN الجديد' : 'Confirm new PIN',
                    ),
                    if (_error != null) ...[
                      const SizedBox(height: 12),
                      Text(
                        _error!,
                        style: const TextStyle(color: AppColors.error),
                      ),
                    ],
                    const SizedBox(height: 16),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: _saving
                            ? null
                            : () =>
                                _save(isSet: isSet, isArabic: isArabic),
                        child: _saving
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(
                                    strokeWidth: 2),
                              )
                            : Text(isArabic ? 'حفظ' : 'Save'),
                      ),
                    ),
                  ],
                ),
              ),
              if (isSet) ...[
                const SizedBox(height: 16),
                AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isArabic ? 'ازالة PIN' : 'Remove PIN',
                        style: Theme.of(context)
                            .textTheme
                            .titleMedium
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        isArabic
                            ? 'ادخل PIN الحالي ثم اضغط ازالة.'
                            : 'Enter the current PIN, then tap Remove.',
                      ),
                      const SizedBox(height: 12),
                      SizedBox(
                        width: double.infinity,
                        child: OutlinedButton(
                          onPressed: _saving
                              ? null
                              : () => _remove(isArabic: isArabic),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: AppColors.error,
                          ),
                          child: Text(isArabic ? 'ازالة PIN' : 'Remove PIN'),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading kiosk PIN',
          onRetry: () => ref.invalidate(kioskPinInfoProvider),
        ),
      ),
    );
  }
}

class _PinField extends StatelessWidget {
  final TextEditingController controller;
  final String label;

  const _PinField({required this.controller, required this.label});

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      obscureText: true,
      keyboardType: TextInputType.number,
      inputFormatters: [
        FilteringTextInputFormatter.digitsOnly,
        LengthLimitingTextInputFormatter(8),
      ],
      decoration: InputDecoration(
        labelText: label,
        hintText: '••••',
        border: const OutlineInputBorder(),
      ),
    );
  }
}
