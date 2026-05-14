import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../providers/classes_provider.dart';

class ClassDetailScreen extends ConsumerWidget {
  final String classId;

  const ClassDetailScreen({super.key, required this.classId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final classDetail = ref.watch(classDetailProvider(classId));
    final attendance = ref.watch(classAttendanceProvider(classId));
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final authState = ref.watch(authProvider).valueOrNull;
    final canCancel = authState?.hasPermission(StaffPermission.cancelClass) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تفاصيل الحصة' : 'Class Details'),
        actions: [
          if (canCancel)
            TextButton(
              onPressed: () => _showCancelDialog(context, ref, isArabic),
              child: Text(
                isArabic ? 'الغاء' : 'Cancel Class',
                style: const TextStyle(color: AppColors.error),
              ),
            ),
        ],
      ),
      body: classDetail.when(
        data: (gymClass) {
          return RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(classDetailProvider(classId));
              ref.invalidate(classAttendanceProvider(classId));
            },
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isArabic && gymClass.nameAr != null
                            ? gymClass.nameAr!
                            : gymClass.name,
                        style: theme.textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 8),
                      _InfoRow(
                        icon: Icons.person,
                        label: isArabic ? 'المدرب' : 'Instructor',
                        value: gymClass.trainerName ?? '—',
                      ),
                      _InfoRow(
                        icon: Icons.access_time,
                        label: isArabic ? 'الوقت' : 'Time',
                        value: _formatTimeRange(gymClass.schedule),
                      ),
                      _InfoRow(
                        icon: Icons.people,
                        label: isArabic ? 'الحضور' : 'Attendance',
                        value: '${gymClass.bookedCount}/${gymClass.capacity}',
                      ),
                      if (gymClass.location != null)
                        _InfoRow(
                          icon: Icons.location_on,
                          label: isArabic ? 'المكان' : 'Location',
                          value: gymClass.location!,
                        ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  isArabic ? 'قائمة الحضور' : 'Attendance List',
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 8),
                attendance.when(
                  data: (members) {
                    if (members.isEmpty) {
                      return Text(
                        isArabic ? 'لا يوجد حضور' : 'No attendees yet',
                        style: theme.textTheme.bodyMedium,
                      );
                    }
                    return Column(
                      children: members
                          .map((m) => ListTile(
                                leading: AvatarWidget(
                                  imageUrl: m.photoUrl,
                                  name: '${m.firstName} ${m.lastName}',
                                  size: 36,
                                ),
                                title: Text('${m.firstName} ${m.lastName}'),
                                subtitle: m.phone != null ? Text(m.phone!) : null,
                              ))
                          .toList(),
                    );
                  },
                  loading: () => const Center(
                    child: CircularProgressIndicator(),
                  ),
                  error: (_, __) => Text(
                    isArabic ? 'تعذر تحميل قائمة الحضور' : 'Failed to load attendance',
                  ),
                ),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل الحصة' : 'Error loading class',
          onRetry: () => ref.invalidate(classDetailProvider(classId)),
        ),
      ),
    );
  }

  Future<void> _showCancelDialog(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'الغاء الحصة' : 'Cancel Class'),
        content: Text(
          isArabic
              ? 'هل انت متاكد من الغاء هذه الحصة؟ سيتم اخطار المسجلين.'
              : 'Are you sure you want to cancel this class? Registered members will be notified.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(isArabic ? 'لا' : 'No'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: Text(
              isArabic ? 'نعم، الغاء' : 'Yes, Cancel',
              style: const TextStyle(color: AppColors.error),
            ),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    final messenger = ScaffoldMessenger.of(context);
    try {
      final gymId = ref.read(currentGymIdProvider);
      if (gymId.isEmpty) {
        throw Exception('No active gym selected');
      }
      final apiClient = ref.read(apiClientProvider);
      await apiClient.delete(ApiEndpoints.gymClassById(gymId, classId));
      ref.invalidate(classDetailProvider(classId));
      ref.invalidate(classesProvider);
      messenger.showSnackBar(
        SnackBar(
          content: Text(isArabic ? 'تم الغاء الحصة' : 'Class cancelled'),
        ),
      );
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(e is ApiException ? e.message : e.toString())));
    }
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Icon(icon, size: 16, color: theme.colorScheme.primary),
          const SizedBox(width: 8),
          Text(label, style: theme.textTheme.bodySmall),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              value,
              style: theme.textTheme.bodyMedium?.copyWith(
                fontWeight: FontWeight.w500,
              ),
              textAlign: TextAlign.end,
            ),
          ),
        ],
      ),
    );
  }
}

/// Formats a [ClassScheduleModel] into "HH:mm – HH:mm" by trimming seconds
/// off the BE's "HH:mm:ss" startTime and adding the duration on top.
String _formatTimeRange(ClassScheduleModel? schedule) {
  if (schedule == null || schedule.startTime.isEmpty) return '—';
  final parts = schedule.startTime.split(':');
  if (parts.length < 2) return schedule.startTime;
  final hh = int.tryParse(parts[0]) ?? 0;
  final mm = int.tryParse(parts[1]) ?? 0;
  final start = '${parts[0]}:${parts[1]}';
  final totalMinutes = hh * 60 + mm + schedule.durationMinutes;
  final endHh = (totalMinutes ~/ 60) % 24;
  final endMm = totalMinutes % 60;
  final end =
      '${endHh.toString().padLeft(2, '0')}:${endMm.toString().padLeft(2, '0')}';
  return '$start – $end';
}
