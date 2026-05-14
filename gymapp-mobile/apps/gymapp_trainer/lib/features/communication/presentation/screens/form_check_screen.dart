import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

class FormCheckScreen extends ConsumerWidget {
  const FormCheckScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تصحيح الحركات' : 'Form Check'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.info.withOpacity(0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  const Icon(Icons.info_outline,
                      color: AppColors.info, size: 18),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      isArabic
                          ? 'يمكنك مراجعة فيديوهات العملاء لتصحيح حركاتهم'
                          : 'Review client videos to correct their form',
                      style: theme.textTheme.bodySmall,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            Expanded(
              child: EmptyState(
                icon: Icons.video_library_outlined,
                title: isArabic
                    ? 'لا توجد فيديوهات معلقة'
                    : 'No pending form checks',
                subtitle: isArabic
                    ? 'سيظهر هنا فيديوهات العملاء التي تحتاج مراجعة'
                    : 'Client videos awaiting your review will appear here',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
