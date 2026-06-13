import 'package:flutter/material.dart';
import 'app_button.dart';

/// PDPL consent dialog shown during registration and in settings.
class ConsentDialog extends StatefulWidget {
  final bool initialDataProcessing;
  final bool initialMarketing;
  final void Function(bool dataProcessing, bool marketing)? onAccept;
  final VoidCallback? onDecline;

  const ConsentDialog({
    super.key,
    this.initialDataProcessing = false,
    this.initialMarketing = false,
    this.onAccept,
    this.onDecline,
  });

  @override
  State<ConsentDialog> createState() => _ConsentDialogState();
}

class _ConsentDialogState extends State<ConsentDialog> {
  late bool _dataProcessing;
  late bool _marketing;

  @override
  void initState() {
    super.initState();
    _dataProcessing = widget.initialDataProcessing;
    _marketing = widget.initialMarketing;
  }

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Dialog(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                isArabic ? 'الموافقة على معالجة البيانات' : 'Data Consent',
                style: theme.textTheme.headlineSmall,
              ),
              const SizedBox(height: 16),
              Text(
                isArabic
                    ? 'وفقا لقانون حماية البيانات الشخصية المصري (PDPL)، نحتاج موافقتك على معالجة بياناتك.'
                    : 'In accordance with the Egyptian Personal Data Protection Law (PDPL), we need your consent to process your data.',
                style: theme.textTheme.bodyMedium,
              ),
              const SizedBox(height: 24),
              // Data processing consent (required)
              CheckboxListTile(
                value: _dataProcessing,
                onChanged: (v) =>
                    setState(() => _dataProcessing = v ?? false),
                title: Text(
                  isArabic
                      ? 'أوافق على معالجة بياناتي الشخصية *'
                      : 'I consent to personal data processing *',
                  style: theme.textTheme.titleSmall,
                ),
                subtitle: Text(
                  isArabic
                      ? 'مطلوب لاستخدام التطبيق. يتضمن بيانات الملف الشخصي، سجل التمارين، وبيانات التقدم.'
                      : 'Required to use the app. Includes profile data, workout history, and progress data.',
                  style: theme.textTheme.bodySmall,
                ),
                controlAffinity: ListTileControlAffinity.leading,
                contentPadding: EdgeInsets.zero,
              ),
              const Divider(),
              // Marketing consent (optional)
              CheckboxListTile(
                value: _marketing,
                onChanged: (v) => setState(() => _marketing = v ?? false),
                title: Text(
                  isArabic
                      ? 'أوافق على تلقي رسائل تسويقية'
                      : 'I consent to marketing communications',
                  style: theme.textTheme.titleSmall,
                ),
                subtitle: Text(
                  isArabic
                      ? 'اختياري. يمكنك إلغاء الاشتراك في أي وقت من الإعدادات.'
                      : 'Optional. You can unsubscribe at any time from Settings.',
                  style: theme.textTheme.bodySmall,
                ),
                controlAffinity: ListTileControlAffinity.leading,
                contentPadding: EdgeInsets.zero,
              ),
              const SizedBox(height: 24),
              Text(
                isArabic
                    ? 'يمكنك تحميل بياناتك أو حذف حسابك في أي وقت من الإعدادات.'
                    : 'You can download your data or delete your account at any time from Settings.',
                style: theme.textTheme.bodySmall?.copyWith(
                  fontStyle: FontStyle.italic,
                ),
              ),
              const SizedBox(height: 24),
              AppButton(
                text: isArabic ? 'موافق ومتابعة' : 'Accept & Continue',
                onPressed: _dataProcessing
                    ? () => widget.onAccept?.call(_dataProcessing, _marketing)
                    : null,
              ),
              const SizedBox(height: 8),
              AppButton.text(
                text: isArabic ? 'رفض' : 'Decline',
                onPressed: widget.onDecline,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
