import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../gamification/presentation/providers/gamification_providers.dart';
import '../../../home/presentation/providers/home_providers.dart';
import '../providers/progress_providers.dart';

class LogWeightScreen extends ConsumerStatefulWidget {
  const LogWeightScreen({super.key});

  @override
  ConsumerState<LogWeightScreen> createState() => _LogWeightScreenState();
}

class _LogWeightScreenState extends ConsumerState<LogWeightScreen> {
  final _weightController = TextEditingController();
  final _notesController = TextEditingController();
  bool _isSaving = false;

  @override
  void dispose() {
    _weightController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تسجيل الوزن' : 'Log Weight'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            AppTextField(
              label: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
              hint: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
              controller: _weightController,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              inputFormatters: [
                FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
              ],
              autofocus: true,
            ),
            const SizedBox(height: 16),
            AppTextField(
              label: isArabic ? 'ملاحظات' : 'Notes',
              controller: _notesController,
              maxLines: 2,
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _isSaving ? null : _save,
                child: _isSaving
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white))
                    : Text(isArabic ? 'حفظ' : 'Save'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _save() async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final weight = double.tryParse(_weightController.text.trim());
    if (weight == null || weight <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'الرجاء إدخال وزن صحيح أكبر من صفر'
            : 'Enter a valid weight greater than 0'),
      ));
      return;
    }
    // Upper bound: guard against fat-finger entries (e.g. 9999) that would
    // skew the weight chart. 500kg exceeds any realistic human weight.
    if (weight > 500) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'الرجاء إدخال وزن واقعي (أقل من 500 كجم)'
            : 'Enter a realistic weight (under 500 kg)'),
      ));
      return;
    }

    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider).valueOrNull;
      final userId = authState?.user?.id;
      final gymId = authState?.currentGymId;
      if (userId == null || gymId == null) {
        throw Exception('No authenticated user or active gym');
      }
      final now = DateTime.now();
      final dateStr = '${now.year.toString().padLeft(4, '0')}-'
          '${now.month.toString().padLeft(2, '0')}-'
          '${now.day.toString().padLeft(2, '0')}';

      final repo = ref.read(progressRepositoryProvider);
      await repo.logBodyMetric({
        'userId': userId,
        'gymId': gymId,
        'date': dateStr,
        'weight': weight,
        'weightUnit': 'kg',
        'source': 'SelfReported',
        'loggedBy': userId,
        if (_notesController.text.isNotEmpty) 'notes': _notesController.text,
      });
      ref.invalidate(progressDashboardDetailProvider);
      ref.invalidate(bodyMetricsProvider);
      // Keep the Home weight tile and leaderboard in sync with the new value.
      ref.invalidate(progressDashboardProvider);
      ref.invalidate(leaderboardProvider);
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) {
        final isArabicErr = Localizations.localeOf(context).languageCode == 'ar';
        final msg = e is ApiException && e.statusCode == 409
            ? (isArabicErr
                ? 'لقد سجلت وزنك اليوم بالفعل'
                : 'Weight already logged for today')
            : (isArabicErr ? 'تعذر حفظ الوزن' : 'Could not save weight');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }
}
