import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/checkin_provider.dart';

class CheckInLogScreen extends ConsumerWidget {
  const CheckInLogScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final logs = ref.watch(checkInLogProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'سجل الحضور' : 'Check-in Log'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code_scanner),
            tooltip: isArabic ? 'وضع الكيوسك' : 'Kiosk Mode',
            onPressed: () => context.go(RoutePaths.kiosk),
          ),
          IconButton(
            icon: const Icon(Icons.edit_note),
            tooltip: isArabic ? 'تسجيل يدوي' : 'Manual Check-in',
            onPressed: () => context.push(RoutePaths.manualCheckIn),
          ),
        ],
      ),
      body: logs.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.login_outlined,
              title: isArabic ? 'لا يوجد حضور اليوم' : 'No check-ins today',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(checkInLogProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final item = items[index];
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: AvatarWidget(
                      imageUrl: item.memberPhotoUrl,
                      name: item.memberName,
                      size: 40,
                    ),
                    title: Text(
                      item.memberName,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    subtitle: Builder(builder: (context) {
                      // BE CheckInMethod enum is PascalCase (QR / MemberNumber /
                      // Manual). The old `item.method == 'qr'` never matched, so
                      // every check-in (incl. QR) rendered as Manual.
                      final method = item.method
                          .toLowerCase()
                          .replaceAll(RegExp(r'[\s_\-]'), '');
                      final IconData icon;
                      final String label;
                      switch (method) {
                        case 'qr':
                          icon = Icons.qr_code;
                          label = 'QR';
                          break;
                        case 'membernumber':
                          icon = Icons.badge_outlined;
                          label = isArabic ? 'رقم العضو' : 'Member number';
                          break;
                        default:
                          icon = Icons.keyboard;
                          label = isArabic ? 'يدوي' : 'Manual';
                      }
                      return Row(
                        children: [
                          Icon(icon, size: 12),
                          const SizedBox(width: 4),
                          Text(label, style: theme.textTheme.labelSmall),
                        ],
                      );
                    }),
                    trailing: Text(
                      DateFormatter.formatTime(
                          DateFormatter.apiUtcToLocal(item.checkedInAt),
                          locale: isArabic ? 'ar' : 'en'),
                      style: theme.textTheme.bodySmall,
                    ),
                  ),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل السجل' : 'Error loading log',
          onRetry: () => ref.invalidate(checkInLogProvider),
        ),
      ),
    );
  }
}
