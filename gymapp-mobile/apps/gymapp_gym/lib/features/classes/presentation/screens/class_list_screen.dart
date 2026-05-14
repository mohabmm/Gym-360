import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/permissions.dart';
import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/classes_provider.dart';

class ClassListScreen extends ConsumerWidget {
  const ClassListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final classes = ref.watch(classesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider).valueOrNull;
    final canCreate = authState?.hasPermission(StaffPermission.createClass) ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الحصص' : 'Classes'),
      ),
      floatingActionButton: canCreate
          ? FloatingActionButton(
              onPressed: () => context.push(RoutePaths.createClass),
              child: const Icon(Icons.add),
            )
          : null,
      body: classes.when(
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.event_outlined,
              title: isArabic ? 'لا توجد حصص' : 'No classes',
              subtitle: isArabic
                  ? 'اضف حصصا لجدولة التمارين'
                  : 'Add classes to schedule workouts',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(classesProvider.future),
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final item = items[index];
                return _ClassCard(
                  gymClass: item,
                  isArabic: isArabic,
                  onTap: () => context.push('/classes/${item.id}'),
                );
              },
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل الحصص' : 'Error loading classes',
          onRetry: () => ref.invalidate(classesProvider),
        ),
      ),
    );
  }
}

class _ClassCard extends StatelessWidget {
  final GymClassModel gymClass;
  final bool isArabic;
  final VoidCallback onTap;

  const _ClassCard({
    required this.gymClass,
    required this.isArabic,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isFull = gymClass.bookedCount >= gymClass.capacity;

    return AppCard(
      margin: const EdgeInsets.only(bottom: 10),
      onTap: onTap,
      child: Row(
        children: [
          Container(
            width: 4,
            height: 60,
            decoration: BoxDecoration(
              color: isFull ? AppColors.error : AppColors.success,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  isArabic && gymClass.nameAr != null
                      ? gymClass.nameAr!
                      : gymClass.name,
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  gymClass.trainerName ?? '',
                  style: theme.textTheme.bodySmall,
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.access_time, size: 12),
                    const SizedBox(width: 4),
                    Text(
                      _formatScheduleTime(gymClass.schedule?.startTime),
                      style: theme.textTheme.labelSmall,
                    ),
                  ],
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                '${gymClass.bookedCount}/${gymClass.capacity}',
                style: theme.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                  color: isFull ? AppColors.error : AppColors.success,
                ),
              ),
              Text(
                _seatsLabel(gymClass.capacity, isArabic),
                style: theme.textTheme.labelSmall,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Arabic plural rules for "seat" noun after capacity number.
String _seatsLabel(int count, bool isArabic) {
  if (!isArabic) return count == 1 ? 'seat' : 'seats';
  if (count == 1) return 'مقعد';
  if (count == 2) return 'مقعدان';
  if (count >= 3 && count <= 10) return 'مقاعد';
  return 'مقعد';
}

/// BE ships `ClassScheduleDto.startTime` as "HH:mm:ss" (TimeOnly); trim for
/// display. Returns an em-dash when no schedule is attached.
String _formatScheduleTime(String? timeStr) {
  if (timeStr == null || timeStr.isEmpty) return '—';
  final parts = timeStr.split(':');
  if (parts.length < 2) return timeStr;
  return '${parts[0]}:${parts[1]}';
}
