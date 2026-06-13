import 'package:flutter/material.dart';
import '../constants/feature_codes.dart';
import '../theme/app_colors.dart';
import 'app_button.dart';

/// Upgrade prompt shown when a user tries to access a gated feature.
/// Consistent design across all apps.
class UpgradePrompt extends StatelessWidget {
  final String featureCode;
  final String requiredPlan;
  final VoidCallback? onUpgrade;
  final VoidCallback? onDismiss;
  final bool compact;

  const UpgradePrompt({
    super.key,
    required this.featureCode,
    required this.requiredPlan,
    this.onUpgrade,
    this.onDismiss,
    this.compact = false,
  });

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    final featureName = _getFeatureDisplayName(featureCode, isArabic);
    final featureIcon = _getFeatureIcon(featureCode);

    if (compact) {
      return Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: AppColors.warning.withOpacity(0.1),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: AppColors.warning.withOpacity(0.3)),
        ),
        child: Row(
          children: [
            Icon(Icons.lock_outline, color: AppColors.warning, size: 20),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                isArabic
                    ? 'متوفر في خطة $requiredPlan'
                    : 'Available on $requiredPlan plan',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: AppColors.warning,
                ),
              ),
            ),
            TextButton(
              onPressed: onUpgrade,
              child: Text(isArabic ? 'ترقية' : 'Upgrade'),
            ),
          ],
        ),
      );
    }

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: theme.colorScheme.primary.withOpacity(0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(
                featureIcon,
                size: 48,
                color: theme.colorScheme.primary,
              ),
            ),
            const SizedBox(height: 24),
            Text(
              isArabic ? 'افتح $featureName' : 'Unlock $featureName',
              textAlign: TextAlign.center,
              style: theme.textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            Text(
              isArabic
                  ? 'هذه الميزة متوفرة في خطة $requiredPlan'
                  : 'Available on the $requiredPlan plan',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurface.withOpacity(0.6),
              ),
            ),
            const SizedBox(height: 32),
            AppButton(
              text: isArabic ? 'ترقية الآن' : 'Upgrade Now',
              onPressed: onUpgrade,
              icon: Icons.rocket_launch_outlined,
              width: 220,
            ),
            const SizedBox(height: 12),
            if (onDismiss != null)
              AppButton.text(
                text: isArabic ? 'ربما لاحقا' : 'Maybe Later',
                onPressed: onDismiss,
                width: 220,
              ),
          ],
        ),
      ),
    );
  }

  static String _getFeatureDisplayName(String code, bool isArabic) {
    final names = <String, Map<String, String>>{
      FeatureCodes.nutritionTracking: {
        'ar': 'تتبع التغذية',
        'en': 'Nutrition Tracking',
      },
      FeatureCodes.mealPlans: {
        'ar': 'خطط الوجبات',
        'en': 'Meal Plans',
      },
      FeatureCodes.leaderboards: {
        'ar': 'لوحة المتصدرين',
        'en': 'Leaderboards',
      },
      FeatureCodes.trainerRatings: {
        'ar': 'تقييمات المدرب',
        'en': 'Trainer Ratings',
      },
      FeatureCodes.gymBranding: {
        'ar': 'العلامة التجارية',
        'en': 'Gym Branding',
      },
      FeatureCodes.advancedAnalytics: {
        'ar': 'التحليلات المتقدمة',
        'en': 'Advanced Analytics',
      },
      FeatureCodes.remoteWorkout: {
        'ar': 'التمرين عن بعد',
        'en': 'Remote Workouts',
      },
      FeatureCodes.customExerciseLibrary: {
        'ar': 'مكتبة التمارين المخصصة',
        'en': 'Custom Exercise Library',
      },
      FeatureCodes.customPermissions: {
        'ar': 'الصلاحيات المخصصة',
        'en': 'Custom Permissions',
      },
      FeatureCodes.progressTracking: {
        'ar': 'تتبع التقدم',
        'en': 'Progress Tracking',
      },
      FeatureCodes.sessionBooking: {
        'ar': 'حجز الجلسات',
        'en': 'Session Booking',
      },
      FeatureCodes.classManagement: {
        'ar': 'إدارة الفصول',
        'en': 'Class Management',
      },
    };

    final entry = names[code];
    if (entry != null) {
      return isArabic ? entry['ar']! : entry['en']!;
    }
    return code.replaceAll('_', ' ');
  }

  static IconData _getFeatureIcon(String code) {
    switch (code) {
      case FeatureCodes.nutritionTracking:
        return Icons.restaurant_outlined;
      case FeatureCodes.mealPlans:
        return Icons.menu_book_outlined;
      case FeatureCodes.leaderboards:
        return Icons.leaderboard_outlined;
      case FeatureCodes.trainerRatings:
        return Icons.star_outline;
      case FeatureCodes.gymBranding:
        return Icons.palette_outlined;
      case FeatureCodes.advancedAnalytics:
        return Icons.analytics_outlined;
      case FeatureCodes.remoteWorkout:
        return Icons.fitness_center_outlined;
      case FeatureCodes.customExerciseLibrary:
        return Icons.library_add_outlined;
      case FeatureCodes.customPermissions:
        return Icons.admin_panel_settings_outlined;
      case FeatureCodes.progressTracking:
        return Icons.trending_up;
      case FeatureCodes.sessionBooking:
        return Icons.calendar_today_outlined;
      case FeatureCodes.classManagement:
        return Icons.groups_outlined;
      default:
        return Icons.lock_outline;
    }
  }
}
