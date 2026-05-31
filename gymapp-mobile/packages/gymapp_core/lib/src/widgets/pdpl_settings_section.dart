import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:path_provider/path_provider.dart';
import '../constants/api_endpoints.dart';
import '../network/api_client.dart';
import '../theme/app_colors.dart';

/// Egypt PDPL user rights panel — drops into any Settings screen.
///
/// Gives the user three controls required by the Egyptian Personal Data
/// Protection Law (Articles 10–12):
///  * **Download my data** — GET `/api/v1/identity/my-data` and write to a
///    JSON file the user can share.
///  * **Delete my account** — DELETE `/api/v1/identity/account` behind a
///    typed-confirmation dialog (user must type "DELETE").
///  * **Manage consents** — hook the caller passes in, typically opens
///    [ConsentDialog] so marketing / data-processing toggles stay in one place.
class PdplSettingsSection extends StatelessWidget {
  final ApiClient apiClient;
  final VoidCallback? onManageConsents;
  final Future<void> Function()? onAfterDeletion;
  final String? titleOverride;

  const PdplSettingsSection({
    super.key,
    required this.apiClient,
    this.onManageConsents,
    this.onAfterDeletion,
    this.titleOverride,
  });

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Container(
      margin: const EdgeInsets.symmetric(vertical: 8),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: theme.dividerColor),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 6),
            child: Row(
              children: [
                Icon(Icons.privacy_tip_rounded, size: 18, color: AppColors.primary),
                const SizedBox(width: 8),
                Text(
                  titleOverride ??
                      (isArabic ? 'الخصوصية وبياناتك' : 'Privacy & Your Data'),
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
            child: Text(
              isArabic
                  ? 'وفقا لقانون حماية البيانات الشخصية المصري، يمكنك الوصول إلى بياناتك أو حذف حسابك في أي وقت.'
                  : 'Under the Egyptian PDPL you can access or delete your data at any time.',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.textTheme.bodySmall?.color?.withOpacity(0.75),
              ),
            ),
          ),
          const Divider(height: 1),
          if (onManageConsents != null)
            _PdplRow(
              icon: Icons.check_circle_outline_rounded,
              label: isArabic ? 'إدارة الموافقات' : 'Manage consents',
              subtitle: isArabic
                  ? 'معالجة البيانات والاتصالات التسويقية'
                  : 'Data processing and marketing communications',
              onTap: onManageConsents!,
            ),
          _PdplRow(
            icon: Icons.download_rounded,
            label: isArabic ? 'تحميل بياناتي' : 'Download my data',
            subtitle: isArabic
                ? 'نسخة JSON من ملفك الشخصي وسجلاتك'
                : 'A JSON copy of your profile and history',
            onTap: () => _downloadData(context, isArabic),
          ),
          _PdplRow(
            icon: Icons.delete_forever_rounded,
            label: isArabic ? 'حذف الحساب' : 'Delete my account',
            subtitle: isArabic
                ? 'إزالة دائمة. لا يمكن التراجع.'
                : 'Permanent removal. This cannot be undone.',
            destructive: true,
            onTap: () => _confirmDelete(context, isArabic),
            isLast: true,
          ),
        ],
      ),
    );
  }

  Future<void> _downloadData(BuildContext context, bool isArabic) async {
    final messenger = ScaffoldMessenger.of(context);
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );
    try {
      final res = await apiClient.dio.get(ApiEndpoints.myData);
      if (!context.mounted) return;
      Navigator.of(context, rootNavigator: true).pop();

      final payload = res.data is String
          ? res.data as String
          : const JsonEncoder.withIndent('  ').convert(res.data);

      if (kIsWeb) {
        await showDialog<void>(
          context: context,
          builder: (ctx) => _DataPreviewDialog(payload: payload, isArabic: isArabic),
        );
        return;
      }

      final dir = await getApplicationDocumentsDirectory();
      final file = File(
        '${dir.path}/gymapp-my-data-${DateTime.now().millisecondsSinceEpoch}.json',
      );
      await file.writeAsString(payload);

      messenger.showSnackBar(
        SnackBar(
          content: Text(
            isArabic
                ? 'تم حفظ بياناتك في: ${file.path}'
                : 'Saved your data to: ${file.path}',
          ),
          duration: const Duration(seconds: 6),
        ),
      );
    } catch (e) {
      if (!context.mounted) return;
      Navigator.of(context, rootNavigator: true).pop();
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            isArabic ? 'تعذر تحميل البيانات: $e' : 'Could not download data: $e',
          ),
          backgroundColor: AppColors.error,
        ),
      );
    }
  }

  Future<void> _confirmDelete(BuildContext context, bool isArabic) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => _DeleteAccountDialog(isArabic: isArabic),
    );
    if (confirmed != true) return;
    if (!context.mounted) return;
    final messenger = ScaffoldMessenger.of(context);

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );
    try {
      await apiClient.dio.delete(ApiEndpoints.deleteAccount);
      if (!context.mounted) return;
      Navigator.of(context, rootNavigator: true).pop();
      await onAfterDeletion?.call();
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            isArabic ? 'تم حذف حسابك.' : 'Your account has been deleted.',
          ),
        ),
      );
    } catch (e) {
      if (!context.mounted) return;
      Navigator.of(context, rootNavigator: true).pop();
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            isArabic ? 'فشل الحذف: $e' : 'Deletion failed: $e',
          ),
          backgroundColor: AppColors.error,
        ),
      );
    }
  }
}

class _PdplRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String subtitle;
  final VoidCallback onTap;
  final bool destructive;
  final bool isLast;

  const _PdplRow({
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.onTap,
    this.destructive = false,
    this.isLast = false,
  });

  @override
  Widget build(BuildContext context) {
    final color = destructive ? AppColors.error : AppColors.primary;
    return Column(
      children: [
        InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: color.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(icon, size: 18, color: color),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        label,
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          color: destructive ? AppColors.error : null,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        subtitle,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_right,
                  size: 20,
                  color: Theme.of(context).hintColor,
                ),
              ],
            ),
          ),
        ),
        if (!isLast) const Divider(height: 1, indent: 16, endIndent: 16),
      ],
    );
  }
}

class _DeleteAccountDialog extends StatefulWidget {
  final bool isArabic;
  const _DeleteAccountDialog({required this.isArabic});

  @override
  State<_DeleteAccountDialog> createState() => _DeleteAccountDialogState();
}

class _DeleteAccountDialogState extends State<_DeleteAccountDialog> {
  final _controller = TextEditingController();
  bool _canConfirm = false;

  static const _confirmWord = 'DELETE';

  @override
  void initState() {
    super.initState();
    _controller.addListener(() {
      final ok = _controller.text.trim().toUpperCase() == _confirmWord;
      if (ok != _canConfirm) setState(() => _canConfirm = ok);
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    return AlertDialog(
      icon: Icon(Icons.warning_amber_rounded, color: AppColors.error, size: 40),
      title: Text(
        isArabic ? 'حذف الحساب نهائيا' : 'Delete account permanently',
        textAlign: TextAlign.center,
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic
                ? 'سيتم حذف جميع بياناتك — الملف الشخصي، التمارين، التقدم، الرسائل — خلال 30 يوما. هذا الإجراء لا يمكن التراجع عنه.'
                : 'All your data — profile, workouts, progress, messages — will be deleted within 30 days. This cannot be undone.',
          ),
          const SizedBox(height: 16),
          Text(
            isArabic
                ? 'اكتب "$_confirmWord" للتأكيد:'
                : 'Type "$_confirmWord" to confirm:',
            style: Theme.of(context).textTheme.bodySmall,
          ),
          const SizedBox(height: 8),
          TextField(
            controller: _controller,
            autofocus: true,
            textCapitalization: TextCapitalization.characters,
            decoration: InputDecoration(
              hintText: _confirmWord,
              isDense: true,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: Text(isArabic ? 'إلغاء' : 'Cancel'),
        ),
        FilledButton(
          onPressed: _canConfirm ? () => Navigator.of(context).pop(true) : null,
          style: FilledButton.styleFrom(backgroundColor: AppColors.error),
          child: Text(isArabic ? 'حذف نهائيا' : 'Delete forever'),
        ),
      ],
    );
  }
}

class _DataPreviewDialog extends StatelessWidget {
  final String payload;
  final bool isArabic;
  const _DataPreviewDialog({required this.payload, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(isArabic ? 'بياناتك' : 'Your data'),
      content: SizedBox(
        width: 520,
        height: 420,
        child: SingleChildScrollView(
          child: SelectableText(
            payload,
            style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(isArabic ? 'إغلاق' : 'Close'),
        ),
      ],
    );
  }
}
