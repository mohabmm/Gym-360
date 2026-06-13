import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Full-screen overlay shown when a gym is suspended.
/// No dismiss button -- only removed when gym_reactivated push received.
/// Different messages for trainee/trainer vs staff apps.
class GymSuspensionOverlay extends StatelessWidget {
  /// Whether this is for the gym staff app (read-only mode vs fully blocked).
  final bool isStaffApp;

  /// Whether this is for the kiosk.
  final bool isKiosk;

  /// Optional child for staff app (read-only mode shows content underneath).
  final Widget? child;

  const GymSuspensionOverlay({
    super.key,
    this.isStaffApp = false,
    this.isKiosk = false,
    this.child,
  });

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    if (isKiosk) {
      return Container(
        color: AppColors.kioskBackground,
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.block,
                size: 80,
                color: AppColors.kioskError,
              ),
              const SizedBox(height: 24),
              Text(
                isArabic
                    ? 'هذه الصالة غير متاحة حاليا'
                    : 'This gym is temporarily unavailable',
                style: const TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.w700,
                  color: AppColors.kioskText,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 12),
              Text(
                isArabic
                    ? 'يرجى التواصل مع إدارة الصالة'
                    : 'Please contact gym management',
                style: TextStyle(
                  fontSize: 18,
                  color: AppColors.kioskText.withOpacity(0.7),
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    if (isStaffApp) {
      // Staff app: read-only mode with banner
      return Stack(
        children: [
          if (child != null) child!,
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: SafeArea(
              child: Container(
                margin: const EdgeInsets.all(12),
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
                decoration: BoxDecoration(
                  color: AppColors.error,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: [
                    BoxShadow(
                      color: AppColors.error.withOpacity(0.3),
                      blurRadius: 8,
                      offset: const Offset(0, 2),
                    ),
                  ],
                ),
                child: Row(
                  children: [
                    const Icon(
                      Icons.warning_amber_rounded,
                      color: AppColors.white,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        isArabic
                            ? 'اشتراكك معلق. تواصل مع دعم GymApp لإعادة التفعيل.'
                            : 'Your subscription is suspended. Contact GymApp support to reactivate.',
                        style: const TextStyle(
                          color: AppColors.white,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      );
    }

    // Trainee/Trainer app: full-screen overlay, no dismiss
    return Container(
      color: Theme.of(context).scaffoldBackgroundColor,
      child: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    color: AppColors.error.withOpacity(0.1),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.block,
                    size: 64,
                    color: AppColors.error,
                  ),
                ),
                const SizedBox(height: 32),
                Text(
                  isArabic ? 'الاشتراك معلق' : 'Subscription Suspended',
                  style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 16),
                Text(
                  isArabic
                      ? 'تم تعليق اشتراك الصالة الرياضية. يرجى التواصل مع إدارة الصالة.'
                      : 'Your gym\'s subscription has been suspended. Please contact your gym management.',
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        color: Theme.of(context)
                            .colorScheme
                            .onSurface
                            .withOpacity(0.7),
                      ),
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Grace period warning banner shown when gym is overdue but not yet suspended.
class GraceWarningBanner extends StatelessWidget {
  final int daysRemaining;

  const GraceWarningBanner({
    super.key,
    required this.daysRemaining,
  });

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.graceWarning.withOpacity(0.15),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppColors.graceWarning.withOpacity(0.5)),
      ),
      child: Row(
        children: [
          const Icon(
            Icons.warning_amber_rounded,
            color: AppColors.graceWarning,
            size: 20,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              isArabic
                  ? 'اشتراكك متأخر. ادفع خلال $daysRemaining يوم لتجنب التعليق.'
                  : 'Your subscription payment is overdue. Pay within $daysRemaining days to avoid suspension.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: AppColors.graceWarning,
                    fontWeight: FontWeight.w500,
                  ),
            ),
          ),
        ],
      ),
    );
  }
}
