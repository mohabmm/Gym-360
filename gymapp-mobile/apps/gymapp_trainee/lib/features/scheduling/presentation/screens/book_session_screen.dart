import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../membership/presentation/providers/membership_providers.dart';
import '../providers/scheduling_providers.dart';

class BookSessionScreen extends ConsumerStatefulWidget {
  const BookSessionScreen({super.key});

  @override
  ConsumerState<BookSessionScreen> createState() => _BookSessionScreenState();
}

class _BookSessionScreenState extends ConsumerState<BookSessionScreen> {
  late DateTime _weekStart;

  @override
  void initState() {
    super.initState();
    _weekStart = _startOfWeek(DateTime.now());
    // Staff-side trainer assignment (quick-assign) is not pushed to mobile,
    // so the FutureProvider would otherwise serve a stale empty list on every
    // re-entry until the user pulls-to-refresh on Membership. Invalidate on
    // mount so the screen always reflects the current assignment.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) ref.invalidate(activePackagesProvider);
    });
  }

  static DateTime _startOfWeek(DateTime d) {
    final date = DateTime(d.year, d.month, d.day);
    return date.subtract(Duration(days: date.weekday - DateTime.monday));
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final auth = ref.watch(authProvider).valueOrNull;
    final gymId = auth?.currentGymId;

    // Trainer assignment is derived from the latest Active package on the
    // backend (TrainerAssignmentLookup). Read it from /me/packages directly so
    // the screen reflects assignments made AFTER login — the cached
    // `gymMemberships[].assignedTrainerId` only refreshes on cold start.
    final packagesAsync = ref.watch(activePackagesProvider);
    final activePackage = packagesAsync.valueOrNull
        ?.where((p) => p.status.toLowerCase() == 'active')
        .fold<SessionPackageModel?>(null, (latest, p) {
      if (latest == null) return p;
      final lhs = p.assignedAt ?? p.createdAt ?? DateTime(0);
      final rhs = latest.assignedAt ?? latest.createdAt ?? DateTime(0);
      return lhs.isAfter(rhs) ? p : latest;
    });
    final trainerId = activePackage?.trainerId.isNotEmpty == true
        ? activePackage!.trainerId
        : auth?.user?.gymMemberships
            ?.where((m) =>
                (gymId == null || m.gymId == gymId) &&
                m.assignedTrainerId != null)
            .map((m) => m.assignedTrainerId)
            .firstWhere((id) => id != null, orElse: () => null);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'احجز جلسة' : 'Book Session'),
        actions: [
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: _weekStart.isAfter(_startOfWeek(DateTime.now()))
                ? () => setState(() =>
                    _weekStart = _weekStart.subtract(const Duration(days: 7)))
                : null,
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: () => setState(
                () => _weekStart = _weekStart.add(const Duration(days: 7))),
          ),
        ],
      ),
      body: packagesAsync.isLoading
          ? const Center(child: CircularProgressIndicator())
          : (trainerId == null || gymId == null)
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  isArabic
                      ? 'لا يوجد مدرب مخصص لك بعد'
                      : 'No trainer assigned yet',
                  textAlign: TextAlign.center,
                ),
              ),
            )
          : Column(
              children: [
                const _RemainingSessionsBanner(),
                Expanded(
                  child: _WeekGrid(
                    trainerId: trainerId,
                    gymId: gymId,
                    weekStart: _weekStart,
                    isArabic: isArabic,
                  ),
                ),
              ],
            ),
    );
  }
}

/// Wrapper around `bookableSessionsProvider` for screens in this file.
int? _remainingPackageSessions(WidgetRef ref) =>
    ref.watch(bookableSessionsProvider);

class _RemainingSessionsBanner extends ConsumerWidget {
  const _RemainingSessionsBanner();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final remaining = _remainingPackageSessions(ref);
    if (remaining == null) {
      return const LinearProgressIndicator(minHeight: 2);
    }
    final color = remaining > 0
        ? theme.colorScheme.primaryContainer
        : theme.colorScheme.errorContainer;
    final fg = remaining > 0
        ? theme.colorScheme.onPrimaryContainer
        : theme.colorScheme.onErrorContainer;
    final text = remaining > 0
        ? (isArabic
            ? 'متبقي $remaining جلسات في باقتك'
            : '$remaining sessions remaining in your package')
        : (isArabic
            ? 'لا توجد جلسات متبقية في باقتك. لا يمكنك الحجز.'
            : 'No remaining sessions in your package. Booking is disabled.');
    return Container(
      width: double.infinity,
      color: color,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      child: Text(text, style: TextStyle(color: fg)),
    );
  }
}

class _WeekGrid extends ConsumerWidget {
  final String trainerId;
  final String gymId;
  final DateTime weekStart;
  final bool isArabic;

  const _WeekGrid({
    required this.trainerId,
    required this.gymId,
    required this.weekStart,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asyncWeek = ref.watch(weeklyAvailabilityProvider((
      trainerId: trainerId,
      gymId: gymId,
      weekStart: weekStart,
    )));

    return asyncWeek.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            isArabic ? 'تعذر تحميل التقويم' : 'Failed to load schedule\n$e',
            textAlign: TextAlign.center,
          ),
        ),
      ),
      data: (week) {
        final now = DateTime.now();
        final todayDate = DateTime(now.year, now.month, now.day);
        final days = week.days
            .where((d) => !d.date.isBefore(todayDate))
            .toList();
        if (days.isEmpty) {
          return Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Text(
                isArabic
                    ? 'انتقل إلى أسبوع لاحق لمشاهدة المواعيد'
                    : 'Move to a future week to see slots',
                textAlign: TextAlign.center,
              ),
            ),
          );
        }
        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(weeklyAvailabilityProvider((
            trainerId: trainerId,
            gymId: gymId,
            weekStart: weekStart,
          ))),
          child: ListView.builder(
            padding: const EdgeInsets.all(12),
            itemCount: days.length,
            itemBuilder: (context, idx) => _DayCard(
              day: days[idx],
              sessionDuration: week.sessionDurationMinutes,
              trainerId: trainerId,
              gymId: gymId,
              weekStart: weekStart,
              isArabic: isArabic,
              now: now,
            ),
          ),
        );
      },
    );
  }
}

class _DayCard extends ConsumerWidget {
  final DayAvailabilityModel day;
  final int sessionDuration;
  final String trainerId;
  final String gymId;
  final DateTime weekStart;
  final bool isArabic;
  final DateTime now;

  const _DayCard({
    required this.day,
    required this.sessionDuration,
    required this.trainerId,
    required this.gymId,
    required this.weekStart,
    required this.isArabic,
    required this.now,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final dateLabel =
        DateFormat('EEE d MMM', isArabic ? 'ar' : 'en').format(day.date);

    Widget body;
    if (day.isBlocked) {
      body = _StatusBanner(
        color: theme.colorScheme.errorContainer,
        textColor: theme.colorScheme.onErrorContainer,
        text: isArabic
            ? 'محظور${day.blockedReason == null ? '' : ' — ${day.blockedReason}'}'
            : 'Blocked${day.blockedReason == null ? '' : ' — ${day.blockedReason}'}',
      );
    } else if (day.isOffDay) {
      body = _StatusBanner(
        color: theme.colorScheme.surfaceVariant,
        textColor: theme.colorScheme.onSurfaceVariant,
        text: isArabic ? 'يوم اجازة' : 'Off day',
      );
    } else {
      final isToday = day.date.year == now.year &&
          day.date.month == now.month &&
          day.date.day == now.day;
      final visibleSlots = isToday
          ? day.slots.where((s) {
              final parts = s.startTime.split(':');
              final h = int.tryParse(parts[0]) ?? 0;
              final m = parts.length > 1 ? int.tryParse(parts[1]) ?? 0 : 0;
              return h > now.hour || (h == now.hour && m > now.minute);
            }).toList()
          : day.slots;
      if (visibleSlots.isEmpty) {
        body = _StatusBanner(
          color: theme.colorScheme.surfaceVariant,
          textColor: theme.colorScheme.onSurfaceVariant,
          text: isArabic ? 'لا مواعيد متبقية اليوم' : 'No remaining slots today',
        );
      } else {
        body = Wrap(
          spacing: 6,
          runSpacing: 6,
          children: visibleSlots
              .map((s) => _SlotChip(
                    slot: s,
                    date: day.date,
                    trainerId: trainerId,
                    gymId: gymId,
                    weekStart: weekStart,
                    duration: sessionDuration,
                    isArabic: isArabic,
                  ))
              .toList(),
        );
      }
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(dateLabel,
                style: theme.textTheme.titleSmall
                    ?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            body,
          ],
        ),
      ),
    );
  }
}

class _StatusBanner extends StatelessWidget {
  final Color color;
  final Color textColor;
  final String text;

  const _StatusBanner(
      {required this.color, required this.textColor, required this.text});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(text, style: TextStyle(color: textColor)),
    );
  }
}

class _SlotChip extends ConsumerWidget {
  final WeekSlotModel slot;
  final DateTime date;
  final String trainerId;
  final String gymId;
  final DateTime weekStart;
  final int duration;
  final bool isArabic;

  const _SlotChip({
    required this.slot,
    required this.date,
    required this.trainerId,
    required this.gymId,
    required this.weekStart,
    required this.duration,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final (bg, fg, label) = switch (slot.status) {
      WeekSlotStatus.available => (
        Colors.green.withOpacity(0.18),
        Colors.green.shade800,
        slot.startTime.substring(0, 5)
      ),
      WeekSlotStatus.bookedByMe => (
        theme.colorScheme.primary.withOpacity(0.20),
        theme.colorScheme.primary,
        '${slot.startTime.substring(0, 5)} • ${isArabic ? 'لي' : 'mine'}'
      ),
      WeekSlotStatus.bookedByOther => (
        theme.colorScheme.error.withOpacity(0.18),
        theme.colorScheme.error,
        '${slot.startTime.substring(0, 5)} • ${isArabic ? 'محجوز' : 'taken'}'
      ),
    };

    final remaining = _remainingPackageSessions(ref);
    final tappable = slot.status == WeekSlotStatus.available &&
        (remaining ?? 0) > 0;

    return Material(
      color: bg,
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: tappable
            ? () => _showBookSheet(context, ref, slot)
            : null,
        child: Padding(
          padding:
              const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Text(label,
              style:
                  TextStyle(color: fg, fontWeight: FontWeight.w600)),
        ),
      ),
    );
  }

  Future<void> _showBookSheet(
      BuildContext context, WidgetRef ref, WeekSlotModel slot) async {
    final result = await showModalBottomSheet<_BookOutcome>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _BookConfirmSheet(
        date: date,
        slot: slot,
        defaultDuration: duration,
        isArabic: isArabic,
      ),
    );
    if (result == null) return;

    try {
      final repo = ref.read(schedulingRepositoryProvider);
      await repo.bookSession(BookSessionRequest(
        trainerId: trainerId,
        date:
            '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}',
        time: slot.startTime,
        durationMinutes: result.durationMinutes,
        sessionType: result.sessionType,
        focus: result.notes,
      ));
      ref.invalidate(weeklyAvailabilityProvider((
        trainerId: trainerId,
        gymId: gymId,
        weekStart: weekStart,
      )));
      ref.invalidate(upcomingSessionsListProvider);
      ref.invalidate(mySessionsAllProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic ? 'تم حجز الجلسة' : 'Session booked'),
        ));
        Navigator.of(context).pop();
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }
}

class _BookOutcome {
  final int durationMinutes;
  final String sessionType;
  final String? notes;

  _BookOutcome({
    required this.durationMinutes,
    required this.sessionType,
    this.notes,
  });
}

class _BookConfirmSheet extends StatefulWidget {
  final DateTime date;
  final WeekSlotModel slot;
  final int defaultDuration;
  final bool isArabic;

  const _BookConfirmSheet({
    required this.date,
    required this.slot,
    required this.defaultDuration,
    required this.isArabic,
  });

  @override
  State<_BookConfirmSheet> createState() => _BookConfirmSheetState();
}

class _BookConfirmSheetState extends State<_BookConfirmSheet> {
  late int _duration;
  String _type = 'PersonalTraining';
  final _notes = TextEditingController();

  @override
  void initState() {
    super.initState();
    _duration = widget.defaultDuration;
  }

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    return Padding(
      padding: EdgeInsets.fromLTRB(
          16, 16, 16, MediaQuery.of(context).viewInsets.bottom + 16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            isArabic ? 'تأكيد الحجز' : 'Confirm booking',
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          Text(
            '${DateFormat('EEE d MMM', isArabic ? 'ar' : 'en').format(widget.date)}'
            ' — ${widget.slot.startTime.substring(0, 5)}',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 16),
          Text(isArabic ? 'مدة الجلسة' : 'Duration'),
          const SizedBox(height: 4),
          SegmentedButton<int>(
            segments: const [
              ButtonSegment(value: 30, label: Text('30')),
              ButtonSegment(value: 60, label: Text('60')),
              ButtonSegment(value: 90, label: Text('90')),
            ],
            selected: {_duration},
            onSelectionChanged: (s) => setState(() => _duration = s.first),
          ),
          const SizedBox(height: 12),
          Text(isArabic ? 'النوع' : 'Type'),
          const SizedBox(height: 4),
          SegmentedButton<String>(
            segments: [
              ButtonSegment(
                  value: 'PersonalTraining',
                  label: Text(isArabic ? 'تدريب' : 'Personal')),
              ButtonSegment(
                  value: 'Assessment',
                  label: Text(isArabic ? 'تقييم' : 'Assess')),
              ButtonSegment(
                  value: 'FollowUp',
                  label: Text(isArabic ? 'متابعة' : 'Follow')),
            ],
            selected: {_type},
            onSelectionChanged: (s) => setState(() => _type = s.first),
          ),
          const SizedBox(height: 12),
          AppTextField(
            label: isArabic ? 'ملاحظات' : 'Notes',
            controller: _notes,
            maxLines: 2,
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(_BookOutcome(
              durationMinutes: _duration,
              sessionType: _type,
              notes: _notes.text.isEmpty ? null : _notes.text,
            )),
            child: Text(isArabic ? 'تأكيد' : 'Confirm'),
          ),
        ],
      ),
    );
  }
}
