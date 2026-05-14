import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

import '../../../../core/providers.dart';

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final themeMode = ref.watch(themeModeProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الاعدادات' : 'Settings'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Language section
          _SettingsSection(
            title: isArabic ? 'اللغة' : 'Language',
            children: [
              _SettingsTile(
                icon: Icons.language,
                title: isArabic ? 'اللغة الحالية' : 'Current Language',
                subtitle: isArabic ? 'العربية' : 'English',
                trailing: Switch(
                  value: isArabic,
                  onChanged: (value) {
                    final locale = value ? const Locale('ar') : const Locale('en');
                    ref.read(localeProvider.notifier).state = locale;
                    final box = Hive.box(StorageKeys.settingsBox);
                    box.put(StorageKeys.locale, locale.languageCode);
                  },
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Theme section
          _SettingsSection(
            title: isArabic ? 'المظهر' : 'Appearance',
            children: [
              _SettingsTile(
                icon: Icons.brightness_6,
                title: isArabic ? 'المظهر' : 'Theme',
                subtitle: _themeModeLabel(themeMode, isArabic),
                onTap: () => _showThemePicker(context, ref, isArabic),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Internal test mode — value comes from the gym (staff app toggles it).
          // Read-only display here; refreshing this screen re-fetches the BE flag.
          Consumer(builder: (context, ref, _) {
            final async = ref.watch(serverTestingModeProvider);
            final enabled = async.maybeWhen(
              data: (v) => v,
              orElse: () => ref.watch(testModeProvider),
            );
            return _SettingsSection(
              title: isArabic ? 'وضع الاختبار' : 'Test Mode',
              children: [
                _SettingsTile(
                  icon: Icons.bug_report_outlined,
                  title: isArabic ? 'وضع الاختبار' : 'Test Mode',
                  subtitle: isArabic
                      ? 'يضبطه فريق الصالة من تطبيق الموظفين'
                      : 'Controlled by gym staff app',
                  trailing: IgnorePointer(
                    child: Switch(value: enabled, onChanged: (_) {}),
                  ),
                  onTap: () => ref.invalidate(serverTestingModeProvider),
                ),
              ],
            );
          }),
          const SizedBox(height: 16),

          // Notifications section
          _SettingsSection(
            title: isArabic ? 'الاشعارات' : 'Notifications',
            children: [
              _SettingsTile(
                icon: Icons.notifications_outlined,
                title: isArabic ? 'اشعارات الحجز' : 'Booking Notifications',
                subtitle: isArabic ? 'تلقي اشعارات الحجوزات الجديدة' : 'Get notified about new bookings',
                trailing: const _NotificationToggle(prefKey: 'notif_bookings'),
              ),
              _SettingsTile(
                icon: Icons.message_outlined,
                title: isArabic ? 'اشعارات الرسائل' : 'Message Notifications',
                subtitle: isArabic ? 'تلقي اشعارات الرسائل الجديدة' : 'Get notified about new messages',
                trailing: const _NotificationToggle(prefKey: 'notif_messages'),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Privacy section (PDPL)
          _SettingsSection(
            title: isArabic ? 'الخصوصية والبيانات' : 'Privacy & Data',
            children: [
              _SettingsTile(
                icon: Icons.download_outlined,
                title: isArabic ? 'تنزيل بياناتي' : 'Download My Data',
                subtitle: isArabic ? 'تصدير نسخة من بياناتك' : 'Export a copy of your data',
                onTap: () => _requestDataExport(context, ref, isArabic),
              ),
              _SettingsTile(
                icon: Icons.delete_forever_outlined,
                title: isArabic ? 'حذف الحساب' : 'Delete Account',
                subtitle: isArabic ? 'حذف حسابك نهائيا' : 'Permanently delete your account',
                titleColor: AppColors.error,
                onTap: () => _confirmDeleteAccount(context, ref, isArabic),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Logout
          AppButton.outlined(
            text: isArabic ? 'تسجيل الخروج' : 'Sign Out',
            onPressed: () => _confirmLogout(context, ref, isArabic),
          ),
        ],
      ),
    );
  }

  String _themeModeLabel(ThemeMode mode, bool isArabic) {
    switch (mode) {
      case ThemeMode.light:
        return isArabic ? 'فاتح' : 'Light';
      case ThemeMode.dark:
        return isArabic ? 'داكن' : 'Dark';
      default:
        return isArabic ? 'تلقائي' : 'System';
    }
  }

  void _showThemePicker(
      BuildContext context, WidgetRef ref, bool isArabic) {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ListTile(
            leading: const Icon(Icons.brightness_5),
            title: Text(isArabic ? 'فاتح' : 'Light'),
            onTap: () {
              ref.read(themeModeProvider.notifier).state = ThemeMode.light;
              Hive.box(StorageKeys.settingsBox)
                  .put(StorageKeys.themeMode, 'light');
              Navigator.pop(ctx);
            },
          ),
          ListTile(
            leading: const Icon(Icons.brightness_3),
            title: Text(isArabic ? 'داكن' : 'Dark'),
            onTap: () {
              ref.read(themeModeProvider.notifier).state = ThemeMode.dark;
              Hive.box(StorageKeys.settingsBox)
                  .put(StorageKeys.themeMode, 'dark');
              Navigator.pop(ctx);
            },
          ),
          ListTile(
            leading: const Icon(Icons.brightness_auto),
            title: Text(isArabic ? 'تلقائي' : 'System'),
            onTap: () {
              ref.read(themeModeProvider.notifier).state = ThemeMode.system;
              Hive.box(StorageKeys.settingsBox)
                  .put(StorageKeys.themeMode, 'system');
              Navigator.pop(ctx);
            },
          ),
        ],
      ),
    );
  }

  Future<void> _requestDataExport(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      await apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.myData,
        parser: (data) => data is Map<String, dynamic> ? data : {},
      );
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(isArabic
                ? 'سيتم ارسال بياناتك عبر البريد الالكتروني'
                : 'Your data will be sent via email'),
          ),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  Future<void> _confirmDeleteAccount(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'حذف الحساب' : 'Delete Account'),
        content: Text(
          isArabic
              ? 'هل انت متاكد؟ سيتم حذف جميع بياناتك نهائيا.'
              : 'Are you sure? All your data will be permanently deleted.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          ElevatedButton(
            style:
                ElevatedButton.styleFrom(backgroundColor: AppColors.error),
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isArabic ? 'حذف نهائيا' : 'Delete Permanently'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      final apiClient = ref.read(apiClientProvider);
      try {
        await apiClient.delete(ApiEndpoints.deleteAccount);
        await ref.read(authProvider.notifier).logout();
      } catch (e) {
        if (context.mounted) {
          ScaffoldMessenger.of(context)
              .showSnackBar(SnackBar(content: Text(e.toString())));
        }
      }
    }
  }

  Future<void> _confirmLogout(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'تسجيل الخروج' : 'Sign Out'),
        content: Text(
          isArabic ? 'هل تريد تسجيل الخروج؟' : 'Are you sure you want to sign out?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isArabic ? 'نعم' : 'Yes'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(authProvider.notifier).logout();
    }
  }
}

class _SettingsSection extends StatelessWidget {
  final String title;
  final List<Widget> children;

  const _SettingsSection({required this.title, required this.children});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: 8, left: 4),
          child: Text(
            title,
            style: theme.textTheme.labelLarge?.copyWith(
              color: theme.colorScheme.primary,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        Card(
          child: Column(
            children: children,
          ),
        ),
      ],
    );
  }
}

class _NotificationToggle extends StatefulWidget {
  final String prefKey;
  const _NotificationToggle({required this.prefKey});

  @override
  State<_NotificationToggle> createState() => _NotificationToggleState();
}

class _NotificationToggleState extends State<_NotificationToggle> {
  late bool _value;

  @override
  void initState() {
    super.initState();
    _value = Hive.box(StorageKeys.settingsBox)
        .get(widget.prefKey, defaultValue: true) as bool;
  }

  @override
  Widget build(BuildContext context) {
    return Switch(
      value: _value,
      onChanged: (v) {
        setState(() => _value = v);
        Hive.box(StorageKeys.settingsBox).put(widget.prefKey, v);
      },
    );
  }
}

class _SettingsTile extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  final Color? titleColor;

  const _SettingsTile({
    required this.icon,
    required this.title,
    this.subtitle,
    this.trailing,
    this.onTap,
    this.titleColor,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListTile(
      leading: Icon(icon, color: titleColor ?? theme.colorScheme.primary),
      title: Text(
        title,
        style: theme.textTheme.bodyMedium?.copyWith(
          fontWeight: FontWeight.w500,
          color: titleColor,
        ),
      ),
      subtitle: subtitle != null
          ? Text(subtitle!, style: theme.textTheme.bodySmall)
          : null,
      trailing: trailing ?? (onTap != null ? const Icon(Icons.chevron_right) : null),
      onTap: onTap,
    );
  }
}
