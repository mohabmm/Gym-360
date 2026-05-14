import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/settings_provider.dart';

class GymSectionsScreen extends ConsumerWidget {
  const GymSectionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final sectionsAsync = ref.watch(gymSectionsProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    // Q31: Sections are enum (Mens/Womens/Mixed), gym-configured
    final sectionOptions = [
      {'value': 'mens', 'label': isArabic ? 'رجال' : "Men's"},
      {'value': 'womens', 'label': isArabic ? 'نساء' : "Women's"},
      {'value': 'mixed', 'label': isArabic ? 'مختلط' : 'Mixed'},
    ];

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'اقسام الصالة' : 'Gym Sections'),
      ),
      body: sectionsAsync.when(
        data: (sections) {
          final activeSections = sections.map((s) => s['type']?.toString()).toSet();

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(
                isArabic
                    ? 'اختر اقسام الصالة المتاحة (Q31)'
                    : 'Select available gym sections (Q31)',
                style: theme.textTheme.bodyMedium,
              ),
              const SizedBox(height: 16),
              ...sectionOptions.map((opt) {
                final isActive = activeSections.contains(opt['value']);
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(
                      opt['label']!,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    value: isActive,
                    onChanged: (_) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text(
                              isArabic
                                  ? 'اقسام الصالة غير متاح حاليا'
                                  : 'Gym sections management not available',
                            ),
                          ),
                        );
                      }
                    },
                  ),
                );
              }),
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading sections',
          onRetry: () => ref.invalidate(gymSectionsProvider),
        ),
      ),
    );
  }
}
