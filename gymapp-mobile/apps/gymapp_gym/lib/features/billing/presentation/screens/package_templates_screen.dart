import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/billing_provider.dart';
import '../../../../core/error_text.dart';

class PackageTemplatesScreen extends ConsumerWidget {
  const PackageTemplatesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final templates = ref.watch(packageTemplatesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'باقات الحصص' : 'Session Packages'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () => _showCreateDialog(context, ref, isArabic),
          ),
        ],
      ),
      body: templates.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.card_giftcard_outlined,
              title: isArabic ? 'لا توجد باقات' : 'No packages',
              subtitle: isArabic ? 'اضف باقات للحصص الخاصة' : 'Add session package templates',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(packageTemplatesProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final pkg = items[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.primary.withOpacity(0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.fitness_center,
                          color: theme.colorScheme.primary),
                    ),
                    title: Text(
                      pkg.name,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    subtitle: Text(
                      '${pkg.sessionCount} ${isArabic ? 'حصة' : 'sessions'}',
                    ),
                    trailing: Text(
                      NumberFormatter.formatCurrency(Egp(pkg.totalPrice)),
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                        color: AppColors.success,
                      ),
                    ),
                  ),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading packages',
          onRetry: () => ref.invalidate(packageTemplatesProvider),
        ),
      ),
    );
  }

  void _showCreateDialog(BuildContext context, WidgetRef ref, bool isArabic) {
    final nameCtrl = TextEditingController();
    final sessionsCtrl = TextEditingController();
    final priceCtrl = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'باقة جديدة' : 'New Package'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            AppTextField(
              controller: nameCtrl,
              label: isArabic ? 'اسم الباقة' : 'Package Name',
            ),
            const SizedBox(height: 12),
            AppTextField(
              controller: sessionsCtrl,
              label: isArabic ? 'عدد الحصص' : 'Sessions',
              keyboardType: TextInputType.number,
            ),
            const SizedBox(height: 12),
            AppTextField(
              controller: priceCtrl,
              label: isArabic ? 'السعر' : 'Price',
              keyboardType: TextInputType.number,
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
              final messenger = ScaffoldMessenger.of(context);
              final name = nameCtrl.text.trim();
              final sessionCount = int.tryParse(sessionsCtrl.text.trim()) ?? 0;
              final totalPrice = double.tryParse(priceCtrl.text.trim()) ?? 0;
              final gymId = ref.read(currentGymIdProvider);

              String? validationError;
              if (gymId.isEmpty) {
                validationError = isArabic
                    ? 'تعذر تحديد النادي الحالي'
                    : 'Current gym is not set';
              } else if (name.isEmpty) {
                validationError =
                    isArabic ? 'ادخل اسم الباقة' : 'Enter a package name';
              } else if (sessionCount <= 0) {
                validationError = isArabic
                    ? 'عدد الحصص يجب ان يكون اكبر من صفر'
                    : 'Sessions must be greater than 0';
              } else if (totalPrice <= 0) {
                validationError = isArabic
                    ? 'السعر يجب ان يكون اكبر من صفر'
                    : 'Price must be greater than 0';
              }

              if (validationError != null) {
                messenger.showSnackBar(
                  SnackBar(content: Text(validationError)),
                );
                return;
              }

              Navigator.pop(ctx);
              try {
                final apiClient = ref.read(apiClientProvider);
                await apiClient.post<void>(
                  ApiEndpoints.packageTemplates,
                  data: {
                    'gymId': gymId,
                    'name': name,
                    'sessionCount': sessionCount,
                    'pricePerSession': totalPrice / sessionCount,
                  },
                  parser: (_) {},
                );
                ref.invalidate(packageTemplatesProvider);
                messenger.showSnackBar(
                  SnackBar(
                    content: Text(
                      isArabic ? 'تم انشاء الباقة' : 'Package created',
                    ),
                  ),
                );
              } catch (e) {
                messenger.showSnackBar(
                  SnackBar(content: Text(staffErrorText(e))),
                );
              }
            },
            child: Text(isArabic ? 'اضافة' : 'Add'),
          ),
        ],
      ),
    );
  }
}
