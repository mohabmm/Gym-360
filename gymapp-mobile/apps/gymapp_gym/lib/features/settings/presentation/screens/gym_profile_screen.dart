import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../../../../core/router.dart';

class GymProfileScreen extends ConsumerWidget {
  const GymProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final gym = ref.watch(activeGymProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final apiClient = ref.read(apiClientProvider);
    final auth = ref.watch(authProvider).valueOrNull;
    bool can(StaffPermission permission) =>
        auth?.hasPermission(permission) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ملف الصالة' : 'Gym Profile'),
      ),
      body: gym.when(
        data: (g) {
          if (g == null) {
            // Account not linked to a gym (or the gym failed to load). Keep
            // sign-out reachable so the user is never stuck in the app.
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                const SizedBox(height: 48),
                Icon(Icons.store_mall_directory_outlined,
                    size: 56, color: theme.colorScheme.outline),
                const SizedBox(height: 16),
                Text(
                  isArabic
                      ? 'هذا الحساب غير مرتبط بصالة'
                      : 'This account is not linked to a gym',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.titleMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  isArabic
                      ? 'اطلب من مالك الصالة اضافتك كموظف ثم سجل الدخول مرة اخرى.'
                      : 'Ask the gym owner to add you as staff, then sign in again.',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium,
                ),
                const SizedBox(height: 32),
                _SettingsTile(
                  icon: Icons.logout,
                  title: isArabic ? 'تسجيل الخروج' : 'Sign Out',
                  iconColor: AppColors.error,
                  titleColor: AppColors.error,
                  onTap: () => _confirmLogout(context, ref, isArabic),
                ),
              ],
            );
          }
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              AppCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        AvatarWidget(
                          imageUrl: g.branding?.logoUrl,
                          name: g.name,
                          size: 64,
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                g.name,
                                style: theme.textTheme.titleMedium?.copyWith(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              if (g.phone != null) Text(g.phone!),
                              if (g.email != null) Text(g.email!),
                            ],
                          ),
                        ),
                      ],
                    ),
                    if (g.address.street != null || g.address.city != null) ...[
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          const Icon(Icons.location_on, size: 16),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              [g.address.street, g.address.city]
                                  .where((s) => s != null)
                                  .join(', '),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (can(StaffPermission.editBranding))
                _SettingsTile(
                  icon: Icons.palette_outlined,
                  title: isArabic ? 'العلامة التجارية' : 'Branding',
                  onTap: () => context.push(RoutePaths.branding),
                ),
              if (can(StaffPermission.editAmenities))
                _SettingsTile(
                  icon: Icons.hotel_outlined,
                  title: isArabic ? 'مرافق الصالة' : 'Gym Amenities',
                  onTap: () => context.push(RoutePaths.amenities),
                ),
              if (can(StaffPermission.editSessionPolicies))
                _SettingsTile(
                  icon: Icons.policy_outlined,
                  title: isArabic ? 'سياسات الحصص' : 'Session Policies',
                  onTap: () => context.push(RoutePaths.sessionPolicies),
                ),
              if (can(StaffPermission.editSections))
                _SettingsTile(
                  icon: Icons.meeting_room_outlined,
                  title: isArabic ? 'اقسام الصالة' : 'Gym Sections',
                  onTap: () => context.push(RoutePaths.gymSections),
                ),
              if (can(StaffPermission.editSeasonalHours))
                _SettingsTile(
                  icon: Icons.wb_sunny_outlined,
                  title: isArabic ? 'ساعات موسمية' : 'Seasonal Hours',
                  onTap: () => context.push(RoutePaths.seasonalHours),
                ),
              _SettingsTile(
                icon: Icons.notifications_outlined,
                title: isArabic ? 'اعدادات الاشعارات' : 'Notification Settings',
                onTap: () => context.push(RoutePaths.notificationSettings),
              ),
              if (can(StaffPermission.editGymProfile))
                _SettingsTile(
                  icon: Icons.lock_outline,
                  title: isArabic ? 'رقم PIN الكيوسك' : 'Kiosk PIN',
                  onTap: () => context.push(RoutePaths.kioskPin),
                ),
              if (can(StaffPermission.editGymProfile))
                _SettingsTile(
                  icon: Icons.bug_report_outlined,
                  title: isArabic ? 'وضع الاختبار' : 'Testing Mode',
                  onTap: () => context.push(RoutePaths.testingMode),
                ),
              const SizedBox(height: 16),
              PdplSettingsSection(
                apiClient: apiClient,
                onAfterDeletion: () async {
                  await ref.read(authProvider.notifier).logout();
                  if (context.mounted) context.go(RoutePaths.login);
                },
              ),
              const SizedBox(height: 16),
              _SettingsTile(
                icon: Icons.logout,
                title: isArabic ? 'تسجيل الخروج' : 'Sign Out',
                iconColor: AppColors.error,
                titleColor: AppColors.error,
                onTap: () => _confirmLogout(context, ref, isArabic),
              ),
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في التحميل' : 'Error loading gym',
          onRetry: () => ref.invalidate(activeGymProvider),
        ),
      ),
    );
  }

  Future<void> _confirmLogout(
    BuildContext context,
    WidgetRef ref,
    bool isArabic,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'تسجيل الخروج' : 'Sign Out'),
        content: Text(isArabic
            ? 'هل تريد تسجيل الخروج من الحساب؟'
            : 'Are you sure you want to sign out?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: Text(
              isArabic ? 'خروج' : 'Sign Out',
              style: const TextStyle(color: AppColors.error),
            ),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    await ref.read(authProvider.notifier).logout();
    if (context.mounted) context.go(RoutePaths.login);
  }
}

class _SettingsTile extends StatelessWidget {
  final IconData icon;
  final String title;
  final VoidCallback onTap;
  final Color? iconColor;
  final Color? titleColor;

  const _SettingsTile({
    required this.icon,
    required this.title,
    required this.onTap,
    this.iconColor,
    this.titleColor,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      margin: const EdgeInsets.only(bottom: 8),
      onTap: onTap,
      child: Row(
        children: [
          Icon(icon, size: 20, color: iconColor),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              title,
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                    color: titleColor,
                  ),
            ),
          ),
          const Icon(Icons.chevron_right),
        ],
      ),
    );
  }
}
