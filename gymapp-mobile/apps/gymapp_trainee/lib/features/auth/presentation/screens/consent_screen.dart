import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/auth_providers.dart';

/// PDPL Consent Screen — shown during registration flow.
/// Egypt PDPL requires explicit data processing consent.
class ConsentScreen extends ConsumerWidget {
  const ConsentScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final regState = ref.watch(registrationProvider);
    final notifier = ref.read(registrationProvider.notifier);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الموافقة على البيانات' : 'Data Consent'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(AppRoutes.register),
        ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Icon(
                Icons.security,
                size: 48,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(height: 16),
              Text(
                isArabic
                    ? 'حماية بياناتك الشخصية'
                    : 'Your Data Privacy',
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                isArabic
                    ? 'وفقا لقانون حماية البيانات الشخصية المصري، نحتاج موافقتك على معالجة بياناتك.'
                    : 'In compliance with the Egypt Personal Data Protection Law (PDPL), we need your consent to process your data.',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.7),
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 32),

              // Data processing consent (required)
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  border: Border.all(
                    color: regState.dataProcessingConsent
                        ? theme.colorScheme.primary
                        : theme.dividerColor,
                  ),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Checkbox(
                      value: regState.dataProcessingConsent,
                      onChanged: (v) => notifier.setDataConsent(v ?? false),
                    ),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  isArabic
                                      ? 'موافقة معالجة البيانات (مطلوب)'
                                      : 'Data Processing Consent (Required)',
                                  style: theme.textTheme.titleSmall?.copyWith(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 2,
                                ),
                                decoration: BoxDecoration(
                                  color: theme.colorScheme.error.withOpacity(0.1),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  isArabic ? 'مطلوب' : 'Required',
                                  style: TextStyle(
                                    fontSize: 11,
                                    color: theme.colorScheme.error,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Text(
                            isArabic
                                ? 'أوافق على جمع ومعالجة بياناتي الشخصية بما في ذلك معلومات اللياقة البدنية والصحة لتوفير خدمات إدارة الصالة الرياضية.'
                                : 'I agree to the collection and processing of my personal data including fitness and health information to provide gym management services.',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // Marketing consent (optional)
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  border: Border.all(color: theme.dividerColor),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Checkbox(
                      value: regState.marketingConsent,
                      onChanged: (v) =>
                          notifier.setMarketingConsent(v ?? false),
                    ),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  isArabic
                                      ? 'موافقة التسويق (اختياري)'
                                      : 'Marketing Consent (Optional)',
                                  style: theme.textTheme.titleSmall?.copyWith(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 2,
                                ),
                                decoration: BoxDecoration(
                                  color: theme.colorScheme.primary
                                      .withOpacity(0.1),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  isArabic ? 'اختياري' : 'Optional',
                                  style: TextStyle(
                                    fontSize: 11,
                                    color: theme.colorScheme.primary,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Text(
                            isArabic
                                ? 'أوافق على تلقي عروض ترويجية ونصائح لياقة بدنية عبر البريد الإلكتروني والإشعارات.'
                                : 'I agree to receive promotional offers and fitness tips via email and notifications.',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              const Spacer(),

              if (regState.error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: Text(
                    regState.error!,
                    style: TextStyle(color: theme.colorScheme.error),
                    textAlign: TextAlign.center,
                  ),
                ),

              AppButton(
                text: isArabic ? 'إنشاء الحساب' : 'Create Account',
                isLoading: regState.isLoading,
                onPressed:
                    regState.dataProcessingConsent && !regState.isLoading
                        ? () async {
                            final success = await ref
                                .read(registrationProvider.notifier)
                                .register();
                            if (success && context.mounted) {
                              context.go(AppRoutes.profileSetup);
                            }
                          }
                        : null,
              ),
              const SizedBox(height: 8),
              Text(
                isArabic
                    ? 'يمكنك تغيير هذه الإعدادات لاحقا من الإعدادات.'
                    : 'You can change these settings later in Settings.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.5),
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
