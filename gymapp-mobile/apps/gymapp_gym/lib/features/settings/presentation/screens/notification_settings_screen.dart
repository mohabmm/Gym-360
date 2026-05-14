import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

// Backend NotificationType camelCase strings used for the 4 toggles.
const _kTypeGeneral = 'general';
const _kTypePayment = 'paymentReceived';
const _kTypePackageLow = 'packageLow';
const _kTypeAnnouncement = 'announcement';

class NotificationSettingsScreen extends ConsumerStatefulWidget {
  const NotificationSettingsScreen({super.key});

  @override
  ConsumerState<NotificationSettingsScreen> createState() =>
      _NotificationSettingsScreenState();
}

class _NotificationSettingsScreenState
    extends ConsumerState<NotificationSettingsScreen> {
  bool _newMemberNotif = true;
  bool _paymentNotif = true;
  bool _lowAttendanceNotif = true;
  bool _checkInNotif = false;
  bool _saving = false;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadPreferences();
  }

  Future<void> _loadPreferences() async {
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      final data = await apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.notificationPreferences,
        queryParameters: gymId.isNotEmpty ? {'gymId': gymId} : null,
        parser: (data) => data as Map<String, dynamic>,
      );
      if (mounted) {
        final prefs = (data['preferences'] as List<dynamic>?) ?? [];
        bool findPush(String type) {
          final entry = prefs
              .cast<Map<String, dynamic>>()
              .where((e) =>
                  (e['notificationType'] as String?)
                      ?.toLowerCase() ==
                  type.toLowerCase())
              .firstOrNull;
          return entry?['pushEnabled'] as bool? ?? true;
        }
        setState(() {
          _newMemberNotif = findPush(_kTypeGeneral);
          _paymentNotif = findPush(_kTypePayment);
          _lowAttendanceNotif = findPush(_kTypePackageLow);
          _checkInNotif = findPush(_kTypeAnnouncement);
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      final userId = ref.read(authProvider).valueOrNull?.user?.id ?? '';
      await apiClient.put<void>(
        ApiEndpoints.notificationPreferences,
        data: {
          'userId': userId,
          if (gymId.isNotEmpty) 'gymId': gymId,
          'preferences': [
            {'notificationType': _kTypeGeneral, 'pushEnabled': _newMemberNotif, 'inAppEnabled': _newMemberNotif},
            {'notificationType': _kTypePayment, 'pushEnabled': _paymentNotif, 'inAppEnabled': _paymentNotif},
            {'notificationType': _kTypePackageLow, 'pushEnabled': _lowAttendanceNotif, 'inAppEnabled': _lowAttendanceNotif},
            {'notificationType': _kTypeAnnouncement, 'pushEnabled': _checkInNotif, 'inAppEnabled': _checkInNotif},
          ],
        },
        parser: (_) {},
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              Localizations.localeOf(context).languageCode == 'ar'
                  ? 'تم حفظ الاعدادات'
                  : 'Settings saved',
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException
            ? e.message
            : (Localizations.localeOf(context).languageCode == 'ar'
                ? 'حدث خطأ. حاول مجددا.'
                : 'An error occurred. Please try again.');
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'اعدادات الاشعارات' : 'Notification Settings'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
        children: [
          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                AppCard(
                  child: Column(
                    children: [
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(isArabic ? 'عضو جديد' : 'New Member'),
                        subtitle: Text(isArabic
                            ? 'اشعار عند تسجيل عضو جديد'
                            : 'Notify when a new member registers'),
                        value: _newMemberNotif,
                        onChanged: (v) => setState(() => _newMemberNotif = v),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(isArabic ? 'دفع جديد' : 'New Payment'),
                        subtitle: Text(isArabic
                            ? 'اشعار عند استلام دفع'
                            : 'Notify when a payment is received'),
                        value: _paymentNotif,
                        onChanged: (v) => setState(() => _paymentNotif = v),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(isArabic ? 'انخفاض الحضور' : 'Low Attendance'),
                        subtitle: Text(isArabic
                            ? 'اشعار عند انخفاض الحضور'
                            : 'Notify when attendance drops'),
                        value: _lowAttendanceNotif,
                        onChanged: (v) => setState(() => _lowAttendanceNotif = v),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(isArabic ? 'تسجيل حضور' : 'Check-In'),
                        subtitle: Text(isArabic
                            ? 'اشعار لكل تسجيل حضور'
                            : 'Notify for every check-in (high volume)'),
                        value: _checkInNotif,
                        onChanged: (v) => setState(() => _checkInNotif = v),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'حفظ الاعدادات' : 'Save Settings',
              isLoading: _saving,
              onPressed: _save,
            ),
          ),
        ],
      ),
    );
  }
}
