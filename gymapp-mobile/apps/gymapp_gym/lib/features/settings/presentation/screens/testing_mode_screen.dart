import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/settings_provider.dart';
import '../../../../core/error_text.dart';

// Owner/manager toggle for the gym-level `early_session_start` feature
// override. Drives the trainer mobile app's read-only Testing Mode indicator.
// Receptionists hit 403 from the BE; we leave the failure to surface as an
// ApiException so the user sees a meaningful error.
class TestingModeScreen extends ConsumerWidget {
  const TestingModeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final gymId = ref.watch(currentGymIdProvider);
    final flag = ref.watch(testingModeProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'وضع الاختبار' : 'Testing Mode'),
      ),
      body: flag.when(
        data: (enabled) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              child: SwitchListTile(
                title: Text(
                  isArabic ? 'تفعيل وضع الاختبار' : 'Enable Testing Mode',
                ),
                subtitle: Text(
                  isArabic
                      ? 'يسمح للمدربين ببدء الجلسة قبل موعدها (للاختبار فقط)'
                      : 'Lets trainers start a session before its scheduled time (QA only)',
                ),
                value: enabled,
                onChanged: (v) async {
                  try {
                    await ref.read(setTestingModeProvider)(gymId, v);
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text(staffErrorText(e))),
                      );
                    }
                  }
                },
              ),
            ),
            const SizedBox(height: 8),
            Text(
              isArabic
                  ? 'تظهر القيمة في تطبيق المدرب كقراءة فقط.'
                  : 'The trainer app shows this value as read-only.',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Failed to load',
          onRetry: () => ref.invalidate(testingModeProvider),
        ),
      ),
    );
  }
}
