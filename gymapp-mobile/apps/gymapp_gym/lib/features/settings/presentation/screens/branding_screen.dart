import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/error_text.dart';

class BrandingScreen extends ConsumerWidget {
  const BrandingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final branding = ref.watch(gymBrandingProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'العلامة التجارية' : 'Branding'),
      ),
      body: branding.when(
        data: (b) {
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isArabic ? 'الشعار الحالي' : 'Current Logo',
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 12),
                    if (b?.logoUrl != null)
                      Center(
                        child: AvatarWidget(
                          imageUrl: b!.logoUrl,
                          name: 'Logo',
                          size: 80,
                        ),
                      )
                    else
                      Center(
                        child: Container(
                          width: 80,
                          height: 80,
                          decoration: BoxDecoration(
                            color: theme.colorScheme.primary.withOpacity(0.1),
                            shape: BoxShape.circle,
                          ),
                          child: Icon(
                            Icons.fitness_center,
                            color: theme.colorScheme.primary,
                            size: 40,
                          ),
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (b != null) ...[
                AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isArabic ? 'الالوان' : 'Colors',
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 12),
                      _ColorRow(
                        label: isArabic ? 'اللون الاساسي' : 'Primary Color',
                        colorHex: b.primaryColor,
                      ),
                      _ColorRow(
                        label: isArabic ? 'اللون الثانوي' : 'Secondary Color',
                        colorHex: b.secondaryColor,
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 16),
              AppButton(
                text: isArabic ? 'تحديث الشعار' : 'Update Logo',
                isOutlined: true,
                onPressed: () => _updateLogo(context, ref, isArabic),
              ),
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading branding',
          onRetry: () => ref.invalidate(gymBrandingProvider),
        ),
      ),
    );
  }
}

Future<void> _updateLogo(
    BuildContext context, WidgetRef ref, bool isArabic) async {
  final urlController = TextEditingController();
  final confirmed = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(isArabic ? 'رابط الشعار' : 'Logo URL'),
      content: TextField(
        controller: urlController,
        decoration: InputDecoration(
          hintText: isArabic ? 'https://...' : 'https://...',
          labelText: isArabic ? 'رابط الصورة' : 'Image URL',
        ),
        keyboardType: TextInputType.url,
        autofocus: true,
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(ctx).pop(false),
          child: Text(isArabic ? 'الغاء' : 'Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.of(ctx).pop(true),
          child: Text(isArabic ? 'حفظ' : 'Save'),
        ),
      ],
    ),
  );
  if (confirmed != true) return;
  final url = urlController.text.trim();
  if (url.isEmpty) return;

  final messenger = ScaffoldMessenger.of(context);
  try {
    final gymId = ref.read(currentGymIdProvider);
    if (gymId.isEmpty) return;
    final apiClient = ref.read(apiClientProvider);
    await apiClient.put<void>(
      ApiEndpoints.gymBranding(gymId),
      data: {'logoUrl': url},
      parser: (_) {},
    );
    ref.invalidate(gymBrandingProvider);
    messenger.showSnackBar(
      SnackBar(content: Text(isArabic ? 'تم تحديث الشعار' : 'Logo updated')),
    );
  } catch (e) {
    messenger.showSnackBar(SnackBar(content: Text(staffErrorText(e))));
  }
}

class _ColorRow extends StatelessWidget {
  final String label;
  final String colorHex;

  const _ColorRow({required this.label, required this.colorHex});

  Color _fromHex(String hex) {
    // BE-supplied color strings can be empty or malformed; int.parse would
    // throw FormatException and crash the branding screen. Fall back to grey.
    final h = hex.replaceAll('#', '').trim();
    final value = int.tryParse('FF$h', radix: 16);
    return value != null ? Color(value) : Colors.grey;
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              color: _fromHex(colorHex),
              borderRadius: BorderRadius.circular(4),
              border: Border.all(color: Colors.black12),
            ),
          ),
          const SizedBox(width: 12),
          Text(label),
          const Spacer(),
          Text(colorHex, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}
