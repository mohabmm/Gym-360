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
                    subtitle: Row(
                      children: [
                        Icon(
                          item.method == 'qr'
                              ? Icons.qr_code
                              : Icons.keyboard,
                          size: 12,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          item.method == 'qr'
                              ? (isArabic ? 'QR' : 'QR')
                              : (isArabic ? 'رقمي' : 'Manual'),
                          style: theme.textTheme.labelSmall,
                        ),
                      ],
                    ),
                    trailing: Text(
                      DateFormatter.formatTime(item.checkedInAt,
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
