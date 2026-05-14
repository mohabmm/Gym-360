import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/settings_provider.dart';

class SeasonalHoursScreen extends ConsumerWidget {
  const SeasonalHoursScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final hoursAsync = ref.watch(gymSeasonalHoursProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ساعات موسمية' : 'Seasonal Hours'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    isArabic
                        ? 'الساعات الموسمية غير متاحة حاليا'
                        : 'Seasonal hours management not available',
                  ),
                ),
              );
            },
          ),
        ],
      ),
      body: hoursAsync.when(
        data: (hours) {
          if (hours.isEmpty) {
            return EmptyState(
              icon: Icons.wb_sunny_outlined,
              title: isArabic ? 'لا توجد تعديلات موسمية' : 'No seasonal overrides',
              subtitle: isArabic
                  ? 'اضف مواعيد خاصة لرمضان وغيره (Q32)'
                  : 'Add special hours for Ramadan and other seasons (Q32)',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(gymSeasonalHoursProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: hours.length,
              itemBuilder: (context, index) {
                final h = hours[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.wb_sunny_outlined),
                    title: Text(
                      h['name']?.toString() ?? (isArabic ? 'موسم' : 'Season'),
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    subtitle: Text(
                      '${h['startDate'] ?? ''} - ${h['endDate'] ?? ''}',
                      style: theme.textTheme.bodySmall,
                    ),
                    trailing: IconButton(
                      icon: const Icon(Icons.delete_outline, color: AppColors.error),
                      onPressed: () async {
                        final gymId = ref.read(currentGymIdProvider);
                        final apiClient = ref.read(apiClientProvider);
                        try {
                          await apiClient.delete(
                            '${ApiEndpoints.gymSeasonalHours(gymId)}/${h['id']}',
                          );
                          ref.invalidate(gymSeasonalHoursProvider);
                        } catch (e) {
                          if (context.mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text(e.toString())),
                            );
                          }
                        }
                      },
                    ),
                  ),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading seasonal hours',
          onRetry: () => ref.invalidate(gymSeasonalHoursProvider),
        ),
      ),
    );
  }

  void _showAddDialog(BuildContext context, WidgetRef ref, bool isArabic) {
    final nameCtrl = TextEditingController();
    final openCtrl = TextEditingController(text: '09:00');
    final closeCtrl = TextEditingController(text: '22:00');

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'اضافة موسم' : 'Add Seasonal Hours'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            AppTextField(
              controller: nameCtrl,
              label: isArabic ? 'اسم الموسم' : 'Season Name',
              hint: isArabic ? 'مثال: رمضان' : 'e.g. Ramadan',
            ),
            const SizedBox(height: 8),
            AppTextField(
              controller: openCtrl,
              label: isArabic ? 'وقت الفتح' : 'Opening Time',
            ),
            const SizedBox(height: 8),
            AppTextField(
              controller: closeCtrl,
              label: isArabic ? 'وقت الاغلاق' : 'Closing Time',
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          TextButton(
            onPressed: () async {
              Navigator.pop(ctx);
              try {
                final gymId = ref.read(currentGymIdProvider);
                final apiClient = ref.read(apiClientProvider);
                await apiClient.post<void>(
                  ApiEndpoints.gymSeasonalHours(gymId),
                  data: {
                    'name': nameCtrl.text.trim(),
                    'openTime': openCtrl.text.trim(),
                    'closeTime': closeCtrl.text.trim(),
                  },
                  parser: (_) {},
                );
                ref.invalidate(gymSeasonalHoursProvider);
              } catch (_) {}
            },
            child: Text(isArabic ? 'اضافة' : 'Add'),
          ),
        ],
      ),
    );
  }
}
