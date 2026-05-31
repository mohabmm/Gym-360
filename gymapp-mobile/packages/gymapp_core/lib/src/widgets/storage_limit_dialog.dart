import 'package:flutter/material.dart';
import 'app_button.dart';
import '../theme/app_colors.dart';

/// Dialog shown when a storage limit is reached.
/// Tells the member to ask their gym owner to upgrade.
class StorageLimitDialog extends StatelessWidget {
  final int currentCount;
  final int maxCount;
  final String resourceType;
  final VoidCallback? onDismiss;

  const StorageLimitDialog({
    super.key,
    required this.currentCount,
    required this.maxCount,
    required this.resourceType,
    this.onDismiss,
  });

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return AlertDialog(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppColors.warning.withOpacity(0.1),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.cloud_off_outlined,
              size: 48,
              color: AppColors.warning,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            isArabic ? 'تم الوصول للحد الأقصى' : 'Limit Reached',
            style: theme.textTheme.headlineSmall,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 8),
          Text(
            isArabic
                ? 'لقد وصلت إلى الحد الأقصى لـ $resourceType ($currentCount/$maxCount). اطلب من الصالة ترقية خطتهم.'
                : 'You\'ve reached your $resourceType limit ($currentCount/$maxCount). Ask your gym to upgrade their plan for more storage.',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurface.withOpacity(0.7),
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 24),
          AppButton(
            text: isArabic ? 'حسنا' : 'OK',
            onPressed: onDismiss ?? () => Navigator.of(context).pop(),
          ),
        ],
      ),
    );
  }

  /// Show the storage limit dialog.
  static Future<void> show(
    BuildContext context, {
    required int currentCount,
    required int maxCount,
    required String resourceType,
  }) {
    return showDialog(
      context: context,
      builder: (_) => StorageLimitDialog(
        currentCount: currentCount,
        maxCount: maxCount,
        resourceType: resourceType,
      ),
    );
  }
}
