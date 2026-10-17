import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../data/models/scheduling_requests.dart';
import '../providers/scheduling_providers.dart';

class ClassScheduleScreen extends StatelessWidget {
  const ClassScheduleScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الحصص الجماعية' : 'Class Schedule'),
      ),
      body: const ClassScheduleView(),
    );
  }
}

/// Body of the class schedule screen, decoupled from [Scaffold] so it can be
/// embedded inside a tabbed parent (e.g. the Schedule tab on the trainee app).
class ClassScheduleView extends ConsumerWidget {
  const ClassScheduleView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final classesAsync = ref.watch(gymClassesProvider);
    final selectedDate = ref.watch(selectedClassDateProvider);

    return Column(
      children: [
        SizedBox(
          height: 80,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            itemCount: 14,
            itemBuilder: (context, index) {
              final date = DateTime.now().add(Duration(days: index));
              final isSelected = _isSameDay(selectedDate, date);
              return Padding(
                padding: const EdgeInsets.symmetric(horizontal: 4),
                child: GestureDetector(
                  onTap: () {
                    ref.read(selectedClassDateProvider.notifier).state = date;
                  },
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 200),
                    width: 56,
                    decoration: BoxDecoration(
                      color: isSelected
                          ? Theme.of(context).colorScheme.primary
                          : Theme.of(context).colorScheme.surface,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: isSelected
                            ? Theme.of(context).colorScheme.primary
                            : Theme.of(context).colorScheme.outline,
                      ),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          DateFormat('EEE', isArabic ? 'ar' : 'en').format(date),
                          style: TextStyle(
                            fontSize: 11,
                            color: isSelected
                                ? Colors.white
                                : Theme.of(context).textTheme.bodySmall?.color,
                          ),
                        ),
                        Text(
                          '${date.day}',
                          style: TextStyle(
                            fontWeight: FontWeight.w700,
                            color: isSelected ? Colors.white : null,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () async => ref.invalidate(gymClassesProvider),
            child: classesAsync.when(
              data: (classes) {
                final sorted = [...classes]..sort((a, b) =>
                    (a.schedule?.startTime ?? '')
                        .compareTo(b.schedule?.startTime ?? ''));
                if (sorted.isEmpty) {
                  return ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      const SizedBox(height: 80),
                      EmptyState(
                        icon: Icons.event_busy,
                        title: isArabic
                            ? 'لا توجد حصص متاحة'
                            : 'No classes available',
                      ),
                    ],
                  );
                }
                return ListView.builder(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  itemCount: sorted.length,
                  itemBuilder: (context, index) => _ClassTile(
                    gymClass: sorted[index],
                    isArabic: isArabic,
                  ),
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => AppErrorWidget(
                message: isArabic
                    ? 'تعذر تحميل الحصص. يرجى المحاولة مرة أخرى.'
                    : 'Could not load classes. Please try again.',
                onRetry: () => ref.invalidate(gymClassesProvider),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

bool _isSameDay(DateTime a, DateTime b) =>
    a.year == b.year && a.month == b.month && a.day == b.day;

class _ClassTile extends ConsumerStatefulWidget {
  final GymClassModel gymClass;
  final bool isArabic;

  const _ClassTile({required this.gymClass, required this.isArabic});

  @override
  ConsumerState<_ClassTile> createState() => _ClassTileState();
}

class _ClassTileState extends ConsumerState<_ClassTile> {
  bool _isLoading = false;

  @override
  Widget build(BuildContext context) {
    final cls = widget.gymClass;
    final isArabic = widget.isArabic;
    final name = isArabic && cls.nameAr != null ? cls.nameAr! : cls.name;
    final spotsLeft = cls.capacity - cls.bookedCount;
    final isFull = spotsLeft <= 0;

    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            SizedBox(
              width: 72,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _formatScheduleTime(cls.schedule?.startTime),
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                  ),
                  Text(
                    '${cls.schedule?.durationMinutes ?? 0} ${isArabic ? "دقيقة" : "min"}',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(name,
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                  Text(cls.trainerName ?? '',
                      style: Theme.of(context).textTheme.bodySmall),
                  const SizedBox(height: 4),
                  Text(
                    isFull
                        ? (isArabic ? 'مكتملة' : 'Full')
                        : '${isArabic ? 'متبقي' : '$spotsLeft spots left'} ${isArabic ? spotsLeft.toString() : ''}',
                    style: TextStyle(
                      fontSize: 12,
                      color: isFull ? Colors.red : Colors.green,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
            if (cls.status.toLowerCase() == 'active' ||
                cls.status.toLowerCase() == 'published')
              FilledButton.tonal(
                onPressed: _isLoading || isFull ? null : _bookClass,
                child: _isLoading
                    ? const SizedBox(
                        height: 16,
                        width: 16,
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(isArabic ? 'احجز' : 'Book'),
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _bookClass() async {
    setState(() => _isLoading = true);
    try {
      final authState = ref.read(authProvider).valueOrNull;
      final gymId = authState?.currentGymId;
      final memberId = authState?.user?.id;
      if (gymId == null || memberId == null) {
        throw Exception('No active gym/member');
      }
      final repo = ref.read(schedulingRepositoryProvider);
      await repo.bookClass(ClassBookingRequest(
        gymId: gymId,
        classId: widget.gymClass.id,
        memberId: memberId,
      ));
      ref.invalidate(gymClassesProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
              content: Text(
                  widget.isArabic ? 'تم الحجز بنجاح' : 'Booked successfully')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }
}

/// BE ships `ClassScheduleDto.startTime` as "HH:mm:ss" (TimeOnly). Trim to
/// "HH:mm" for display; return an em-dash if missing rather than crashing.
String _formatScheduleTime(String? timeStr) {
  if (timeStr == null || timeStr.isEmpty) return '—';
  final parts = timeStr.split(':');
  if (parts.length < 2) return timeStr;
  return '${parts[0]}:${parts[1]}';
}
