import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

import '../../../../core/providers.dart';
import '../providers/settings_providers.dart';

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final user = authState.valueOrNull?.user;
    final themeMode = ref.watch(themeModeProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الإعدادات' : 'Settings'),
      ),
      body: ListView(
        children: [
          // User info
          if (user != null)
            InkWell(
              onTap: () => _showProfileSheet(context, ref, user, isArabic),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    AvatarWidget(
                        imageUrl: user.photoUrl,
                        name: user.firstName,
                        size: 56),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            '${user.firstName} ${user.lastName}',
                            style: Theme.of(context)
                                .textTheme
                                .titleMedium
                                ?.copyWith(fontWeight: FontWeight.w600),
                          ),
                          Text(user.email,
                              style: Theme.of(context).textTheme.bodySmall),
                        ],
                      ),
                    ),
                    const Icon(Icons.chevron_right),
                  ],
                ),
              ),
            ),

          const Divider(height: 1),

          // Language
          _SectionHeader(label: isArabic ? 'التطبيق' : 'App'),
          ListTile(
            leading: const Icon(Icons.language),
            title: Text(isArabic ? 'اللغة' : 'Language'),
            subtitle: Text(isArabic ? 'العربية' : 'English'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => _pickLanguage(context, ref, isArabic),
          ),

          // Theme
          ListTile(
            leading: const Icon(Icons.brightness_6_outlined),
            title: Text(isArabic ? 'المظهر' : 'Theme'),
            subtitle: Text(_themeLabel(themeMode, isArabic)),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => _pickTheme(context, ref, themeMode, isArabic),
          ),

          // Unit system
          ListTile(
            leading: const Icon(Icons.straighten),
            title: Text(isArabic ? 'نظام القياس' : 'Unit System'),
            subtitle: Text(_unitLabel(ref.watch(unitSystemProvider), isArabic)),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => _pickUnitSystem(
              context,
              ref,
              ref.watch(unitSystemProvider),
              isArabic,
            ),
          ),

          // Internal test mode (QA only)
          SwitchListTile(
            secondary: const Icon(Icons.bug_report_outlined),
            title: Text(isArabic ? 'وضع الاختبار' : 'Test Mode'),
            subtitle: Text(isArabic
                ? 'تجاوز قيود الوقت (للاختبار فقط)'
                : 'Bypass time-based limits (QA only)'),
            value: ref.watch(testModeProvider),
            onChanged: (v) async {
              ref.read(testModeProvider.notifier).state = v;
              await Hive.box(StorageKeys.settingsBox)
                  .put(StorageKeys.testMode, v);
            },
          ),

          // Notifications
          _SectionHeader(
              label: isArabic ? 'الإشعارات' : 'Notifications'),
          const _NotificationPreferencesSection(),

          // PDPL Data Privacy
          _SectionHeader(
              label: isArabic ? 'الخصوصية والبيانات' : 'Privacy & Data'),

          ListTile(
            leading: const Icon(Icons.privacy_tip_outlined),
            title: Text(isArabic ? 'إعدادات الموافقة' : 'Consent Settings'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => _showConsentSettings(context, ref, isArabic),
          ),
          ListTile(
            leading: const Icon(Icons.download_outlined),
            title: Text(isArabic ? 'تنزيل بياناتي' : 'Download My Data'),
            onTap: () => _downloadMyData(context, ref, isArabic),
          ),
          ListTile(
            leading:
                const Icon(Icons.delete_forever_outlined, color: Colors.red),
            title: Text(
              isArabic ? 'حذف الحساب' : 'Delete Account',
              style: const TextStyle(color: Colors.red),
            ),
            onTap: () => _confirmDeleteAccount(context, ref, isArabic),
          ),

          const Divider(),

          // Logout
          ListTile(
            leading: const Icon(Icons.logout, color: Colors.red),
            title: Text(
              isArabic ? 'تسجيل الخروج' : 'Logout',
              style: const TextStyle(color: Colors.red),
            ),
            onTap: () => _logout(context, ref, isArabic),
          ),

          const SizedBox(height: 32),
        ],
      ),
    );
  }

  String _themeLabel(ThemeMode mode, bool isArabic) {
    switch (mode) {
      case ThemeMode.light:
        return isArabic ? 'فاتح' : 'Light';
      case ThemeMode.dark:
        return isArabic ? 'داكن' : 'Dark';
      case ThemeMode.system:
        return isArabic ? 'تلقائي' : 'System';
    }
  }

  String _unitLabel(UnitSystem unit, bool isArabic) {
    return unit == UnitSystem.imperial
        ? (isArabic ? 'إمبريالي' : 'Imperial')
        : (isArabic ? 'متري' : 'Metric');
  }

  Future<void> _pickLanguage(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final code = isArabic ? 'ar' : 'en';
    final picked = await showModalBottomSheet<String>(
      context: context,
      builder: (ctx) => _RadioSheet<String>(
        title: isArabic ? 'اللغة' : 'Language',
        groupValue: code,
        options: const [
          ('ar', 'العربية'),
          ('en', 'English'),
        ],
      ),
    );
    if (picked != null) {
      ref.read(localeProvider.notifier).state = Locale(picked);
      final box = Hive.box(StorageKeys.settingsBox);
      await box.put(StorageKeys.locale, picked);
    }
  }

  Future<void> _pickTheme(BuildContext context, WidgetRef ref,
      ThemeMode current, bool isArabic) async {
    final picked = await showModalBottomSheet<ThemeMode>(
      context: context,
      builder: (ctx) => _RadioSheet<ThemeMode>(
        title: isArabic ? 'المظهر' : 'Theme',
        groupValue: current,
        options: [
          (ThemeMode.system, isArabic ? 'تلقائي' : 'System'),
          (ThemeMode.light, isArabic ? 'فاتح' : 'Light'),
          (ThemeMode.dark, isArabic ? 'داكن' : 'Dark'),
        ],
      ),
    );
    if (picked != null) {
      ref.read(themeModeProvider.notifier).state = picked;
      final box = Hive.box(StorageKeys.settingsBox);
      await box.put(
        StorageKeys.themeMode,
        picked == ThemeMode.light
            ? 'light'
            : picked == ThemeMode.dark
                ? 'dark'
                : 'system',
      );
    }
  }

  Future<void> _pickUnitSystem(BuildContext context, WidgetRef ref,
      UnitSystem current, bool isArabic) async {
    final picked = await showModalBottomSheet<UnitSystem>(
      context: context,
      builder: (ctx) => _RadioSheet<UnitSystem>(
        title: isArabic ? 'نظام القياس' : 'Unit System',
        groupValue: current,
        options: [
          (UnitSystem.metric, isArabic ? 'متري' : 'Metric'),
          (UnitSystem.imperial, isArabic ? 'إمبريالي' : 'Imperial'),
        ],
      ),
    );
    if (picked != null) {
      ref.read(unitSystemProvider.notifier).state = picked;
      final box = Hive.box(StorageKeys.settingsBox);
      await box.put(
        StorageKeys.unitSystem,
        picked == UnitSystem.imperial ? 'imperial' : 'metric',
      );
    }
  }

  void _showConsentSettings(
      BuildContext context, WidgetRef ref, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => _ConsentSettingsSheet(isArabic: isArabic),
    );
  }

  void _showProfileSheet(
      BuildContext context, WidgetRef ref, UserModel user, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => _ProfileSheet(user: user, isArabic: isArabic),
    );
  }

  Future<void> _downloadMyData(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(
            isArabic ? 'تنزيل بياناتك' : 'Download Your Data'),
        content: Text(isArabic
            ? 'سيتم إرسال نسخة من بياناتك إلى بريدك الإلكتروني. هل تريد المتابعة؟'
            : 'A copy of your data will be sent to your email. Continue?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: Text(isArabic ? 'إلغاء' : 'Cancel')),
          FilledButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: Text(isArabic ? 'تأكيد' : 'Confirm')),
        ],
      ),
    );

    if (confirm != true || !context.mounted) return;

    try {
      final repo = ref.read(settingsRepositoryProvider);
      await repo.downloadMyData();
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'تم إرسال بياناتك إلى بريدك الإلكتروني'
              : 'Your data has been sent to your email'),
        ));
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
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(
            isArabic ? 'حذف الحساب' : 'Delete Account',
            style: const TextStyle(color: Colors.red)),
        content: Text(isArabic
            ? 'هذا الإجراء لا يمكن التراجع عنه. سيتم حذف جميع بياناتك نهائياً. هل أنت متأكد؟'
            : 'This action cannot be undone. All your data will be permanently deleted. Are you sure?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: Text(isArabic ? 'إلغاء' : 'Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style:
                TextButton.styleFrom(foregroundColor: Colors.red),
            child: Text(isArabic ? 'حذف نهائياً' : 'Delete Forever'),
          ),
        ],
      ),
    );

    if (confirm != true || !context.mounted) return;

    try {
      final repo = ref.read(settingsRepositoryProvider);
      await repo.deleteAccount();
      await ref.read(authProvider.notifier).logout();
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  Future<void> _logout(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'تسجيل الخروج' : 'Logout'),
        content: Text(isArabic ? 'هل تريد تسجيل الخروج؟' : 'Are you sure?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: Text(isArabic ? 'إلغاء' : 'Cancel')),
          FilledButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: Text(isArabic ? 'خروج' : 'Logout')),
        ],
      ),
    );

    if (confirm != true) return;
    await ref.read(authProvider.notifier).logout();
  }
}

class _RadioSheet<T> extends StatelessWidget {
  final String title;
  final T groupValue;
  final List<(T, String)> options;

  const _RadioSheet({
    required this.title,
    required this.groupValue,
    required this.options,
  });

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              child: Text(
                title,
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
            ),
            ...options.map(
              (entry) => RadioListTile<T>(
                title: Text(entry.$2),
                value: entry.$1,
                groupValue: groupValue,
                onChanged: (v) => Navigator.of(context).pop<T>(v),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  final String label;
  const _SectionHeader({required this.label});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
      child: Text(
        label,
        style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: Theme.of(context).colorScheme.primary,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.8,
            ),
      ),
    );
  }
}

class _NotificationPreferencesSection extends ConsumerStatefulWidget {
  const _NotificationPreferencesSection();

  @override
  ConsumerState<_NotificationPreferencesSection> createState() =>
      _NotificationPreferencesSectionState();
}

class _NotificationPreferencesSectionState
    extends ConsumerState<_NotificationPreferencesSection> {
  NotificationPreferenceModel? _prefs;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    if (_prefs == null) {
      return ListTile(
        leading: const Icon(Icons.notifications_outlined),
        title: Text(isArabic
            ? 'تفضيلات الإشعارات'
            : 'Notification Preferences'),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => _loadAndShow(context, isArabic),
      );
    }

    return const SizedBox.shrink();
  }

  Future<void> _loadAndShow(BuildContext context, bool isArabic) async {
    final messenger = ScaffoldMessenger.of(context);
    final authState = ref.read(authProvider).valueOrNull;
    final userId = authState?.user?.id;
    final gymId = authState?.currentGymId;
    if (userId == null) {
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'يرجى تسجيل الدخول أولاً'
            : 'Sign in required'),
      ));
      return;
    }
    try {
      final apiClient = ref.read(apiClientProvider);
      final prefs = await apiClient.get<NotificationPreferenceModel>(
        ApiEndpoints.notificationPreferences,
        queryParameters: {
          'userId': userId,
          if (gymId != null) 'gymId': gymId,
        },
        parser: (data) => _parsePrefsFromBackend(
            data as Map<String, dynamic>, userId),
      );
      if (!context.mounted) return;
      await showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        builder: (ctx) => _NotificationPreferencesSheet(
          initial: prefs,
          isArabic: isArabic,
          onSave: (updated) async {
            await apiClient.putVoid(
              ApiEndpoints.notificationPreferences,
              data: _prefsToBackendJson(updated, userId, gymId),
            );
          },
        ),
      );
    } catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'تعذّر تحميل التفضيلات'
            : 'Could not load preferences'),
      ));
    }
  }

  static NotificationPreferenceModel _parsePrefsFromBackend(
      Map<String, dynamic> json, String userId) {
    final list = (json['preferences'] as List<dynamic>? ?? [])
        .cast<Map<String, dynamic>>();

    bool get(String type) {
      final match = list.firstWhere(
        (e) => (e['notificationType'] as String?) == type,
        orElse: () => const {'pushEnabled': true},
      );
      return match['pushEnabled'] as bool? ?? true;
    }

    return NotificationPreferenceModel(
      id: userId,
      userId: userId,
      sessionReminders: get('SessionReminder'),
      expiryWarnings: get('MembershipExpiry'),
      announcements: get('Announcement'),
      chatMessages: get('Message'),
      promotions: get('General'),
      workoutReminders: get('WorkoutAssigned'),
      progressReminders: get('Achievement'),
    );
  }

  static Map<String, dynamic> _prefsToBackendJson(
      NotificationPreferenceModel prefs, String userId, String? gymId) {
    Map<String, dynamic> pref(String type, bool enabled) =>
        {'notificationType': type, 'pushEnabled': enabled, 'inAppEnabled': enabled};

    return {
      if (gymId != null) 'gymId': gymId,
      'preferences': [
        pref('SessionReminder', prefs.sessionReminders),
        pref('MembershipExpiry', prefs.expiryWarnings),
        pref('Announcement', prefs.announcements),
        pref('Message', prefs.chatMessages),
        pref('General', prefs.promotions),
        pref('WorkoutAssigned', prefs.workoutReminders),
        pref('Achievement', prefs.progressReminders),
      ],
      'quietHoursStart': null,
      'quietHoursEnd': null,
    };
  }
}

class _NotificationPreferencesSheet extends StatefulWidget {
  final NotificationPreferenceModel initial;
  final bool isArabic;
  final Future<void> Function(NotificationPreferenceModel) onSave;

  const _NotificationPreferencesSheet({
    required this.initial,
    required this.isArabic,
    required this.onSave,
  });

  @override
  State<_NotificationPreferencesSheet> createState() =>
      _NotificationPreferencesSheetState();
}

class _NotificationPreferencesSheetState
    extends State<_NotificationPreferencesSheet> {
  late NotificationPreferenceModel _prefs = widget.initial;
  bool _saving = false;

  Future<void> _update(NotificationPreferenceModel next) async {
    setState(() {
      _prefs = next;
      _saving = true;
    });
    try {
      await widget.onSave(next);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isArabic ? 'تفضيلات الإشعارات' : 'Notification Preferences',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            if (_saving) const LinearProgressIndicator(),
            SwitchListTile(
              title: Text(isArabic ? 'تذكيرات الجلسات' : 'Session Reminders'),
              value: _prefs.sessionReminders,
              onChanged: (v) =>
                  _update(_prefs.copyWith(sessionReminders: v)),
            ),
            SwitchListTile(
              title: Text(isArabic ? 'تذكيرات التمارين' : 'Workout Reminders'),
              value: _prefs.workoutReminders,
              onChanged: (v) =>
                  _update(_prefs.copyWith(workoutReminders: v)),
            ),
            SwitchListTile(
              title: Text(isArabic ? 'تذكيرات التقدم' : 'Progress Reminders'),
              value: _prefs.progressReminders,
              onChanged: (v) =>
                  _update(_prefs.copyWith(progressReminders: v)),
            ),
            SwitchListTile(
              title: Text(isArabic ? 'الإعلانات' : 'Announcements'),
              value: _prefs.announcements,
              onChanged: (v) => _update(_prefs.copyWith(announcements: v)),
            ),
            SwitchListTile(
              title: Text(isArabic ? 'الرسائل' : 'Chat Messages'),
              value: _prefs.chatMessages,
              onChanged: (v) => _update(_prefs.copyWith(chatMessages: v)),
            ),
            SwitchListTile(
              title: Text(isArabic ? 'تنبيهات الانتهاء' : 'Expiry Warnings'),
              value: _prefs.expiryWarnings,
              onChanged: (v) => _update(_prefs.copyWith(expiryWarnings: v)),
            ),
            SwitchListTile(
              title: Text(isArabic ? 'العروض' : 'Promotions'),
              value: _prefs.promotions,
              onChanged: (v) => _update(_prefs.copyWith(promotions: v)),
            ),
          ],
        ),
      ),
    );
  }
}

/// PDPL consent categories presented to the trainee. The backend only exposes
/// a POST /identity/consent endpoint (no GET), so we render a static list of
/// the consent types the product supports; toggling a row records the user's
/// choice through the existing POST.
class _ConsentOption {
  final String type;
  final String labelEn;
  final String labelAr;
  final String descriptionEn;
  final String descriptionAr;

  const _ConsentOption({
    required this.type,
    required this.labelEn,
    required this.labelAr,
    required this.descriptionEn,
    required this.descriptionAr,
  });
}

const _consentOptions = <_ConsentOption>[
  _ConsentOption(
    type: 'Marketing',
    labelEn: 'Marketing messages',
    labelAr: 'الرسائل التسويقية',
    descriptionEn:
        'Promotions, offers, and product announcements from your gym.',
    descriptionAr: 'العروض الترويجية وإعلانات المنتجات من النادي.',
  ),
  _ConsentOption(
    type: 'Analytics',
    labelEn: 'Usage analytics',
    labelAr: 'تحليلات الاستخدام',
    descriptionEn: 'Anonymous usage data that helps us improve the app.',
    descriptionAr: 'بيانات استخدام مجهولة تساعدنا في تحسين التطبيق.',
  ),
  _ConsentOption(
    type: 'ThirdPartySharing',
    labelEn: 'Share data with trainer',
    labelAr: 'مشاركة البيانات مع المدرب',
    descriptionEn:
        'Allow your trainer to see progress, measurements, and logs.',
    descriptionAr: 'السماح لمدربك برؤية التقدم والقياسات والسجلات.',
  ),
  _ConsentOption(
    type: 'LeaderboardVisibility',
    labelEn: 'Appear on leaderboards',
    labelAr: 'الظهور في قوائم المتصدرين',
    descriptionEn: 'Show my name on public leaderboards at my gym.',
    descriptionAr: 'إظهار اسمي في قوائم المتصدرين داخل النادي.',
  ),
];

class _ConsentSettingsSheet extends ConsumerStatefulWidget {
  final bool isArabic;
  const _ConsentSettingsSheet({required this.isArabic});

  @override
  ConsumerState<_ConsentSettingsSheet> createState() =>
      _ConsentSettingsSheetState();
}

class _ConsentSettingsSheetState extends ConsumerState<_ConsentSettingsSheet> {
  final Map<String, bool> _state = {
    for (final o in _consentOptions) o.type: false,
  };
  final Set<String> _saving = <String>{};

  Future<void> _toggle(_ConsentOption option, bool value) async {
    setState(() {
      _state[option.type] = value;
      _saving.add(option.type);
    });
    try {
      final repo = ref.read(settingsRepositoryProvider);
      await repo.updateConsent(
        option.type,
        value,
        consentText: widget.isArabic ? option.descriptionAr : option.descriptionEn,
      );
    } catch (e) {
      if (mounted) {
        setState(() => _state[option.type] = !value);
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _saving.remove(option.type));
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    // Using a plain scrollable column instead of DraggableScrollableSheet
    // because the draggable variant was leaving the list area collapsed in
    // some cases (see bug #11); eager layout with ListView.shrinkWrap is
    // robust across the tiny-screen and keyboard-open edge cases.
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.only(
          left: 16,
          right: 16,
          top: 12,
          bottom: MediaQuery.of(context).viewInsets.bottom + 16,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              isArabic ? 'إعدادات الموافقة' : 'Consent Settings',
              style: Theme.of(context)
                  .textTheme
                  .titleMedium
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 4),
            Text(
              isArabic
                  ? 'يمكنك التحكم في كيفية استخدام بياناتك'
                  : 'Control how your data is used',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 8),
            Flexible(
              child: ListView.separated(
                shrinkWrap: true,
                itemCount: _consentOptions.length,
                separatorBuilder: (_, __) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final option = _consentOptions[i];
                  final isGranted = _state[option.type] ?? false;
                  final isBusy = _saving.contains(option.type);
                  return SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(isArabic ? option.labelAr : option.labelEn),
                    subtitle: Text(
                      isArabic
                          ? option.descriptionAr
                          : option.descriptionEn,
                    ),
                    value: isGranted,
                    onChanged:
                        isBusy ? null : (v) => _toggle(option, v),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Read-only profile summary shown from the Settings → user-card tap.
/// Edits live in the dedicated profile-edit flow; this sheet is the
/// discoverable entry point when the user taps their name/email row.
class _ProfileSheet extends StatelessWidget {
  final UserModel user;
  final bool isArabic;

  const _ProfileSheet({required this.user, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    String fmtDate(DateTime? d) {
      if (d == null) return '—';
      return '${d.year.toString().padLeft(4, '0')}-'
          '${d.month.toString().padLeft(2, '0')}-'
          '${d.day.toString().padLeft(2, '0')}';
    }

    final primaryMembership = user.gymMemberships?.firstOrNull;

    return SafeArea(
      child: Padding(
        padding: EdgeInsets.only(
          left: 16,
          right: 16,
          top: 12,
          bottom: MediaQuery.of(context).viewInsets.bottom + 16,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                AvatarWidget(
                  imageUrl: user.photoUrl,
                  name: user.firstName,
                  size: 56,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${user.firstName} ${user.lastName}',
                        style: Theme.of(context)
                            .textTheme
                            .titleMedium
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      Text(user.email,
                          style: Theme.of(context).textTheme.bodySmall),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _ProfileRow(
              label: isArabic ? 'رقم الهاتف' : 'Phone',
              value: user.phone ?? '—',
            ),
            _ProfileRow(
              label: isArabic ? 'الجنس' : 'Gender',
              value: user.gender ?? '—',
            ),
            _ProfileRow(
              label: isArabic ? 'تاريخ الميلاد' : 'Date of birth',
              value: fmtDate(user.dateOfBirth),
            ),
            _ProfileRow(
              label: isArabic ? 'الطول' : 'Height',
              value: user.height != null
                  ? '${user.height!.toStringAsFixed(0)} cm'
                  : '—',
            ),
            _ProfileRow(
              label: isArabic ? 'الوزن' : 'Weight',
              value: user.weight != null
                  ? '${user.weight!.toStringAsFixed(1)} kg'
                  : '—',
            ),
            if (primaryMembership?.memberNumber != null)
              _ProfileRow(
                label: isArabic ? 'رقم العضوية' : 'Member number',
                value: primaryMembership!.memberNumber!,
              ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }
}

class _ProfileRow extends StatelessWidget {
  final String label;
  final String value;

  const _ProfileRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(
            flex: 2,
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.w500,
                  ),
            ),
          ),
        ],
      ),
    );
  }
}
