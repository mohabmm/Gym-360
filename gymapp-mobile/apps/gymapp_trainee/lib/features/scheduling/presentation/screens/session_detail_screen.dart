import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/scheduling_providers.dart';
import '../session_status_l10n.dart';

class SessionDetailScreen extends ConsumerWidget {
  final String sessionId;
  const SessionDetailScreen({super.key, required this.sessionId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final asyncSession = ref.watch(sessionDetailProvider(sessionId));

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تفاصيل الجلسة' : 'Session details'),
      ),
      body: asyncSession.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: e.toString(),
          onRetry: () => ref.invalidate(sessionDetailProvider(sessionId)),
        ),
        data: (s) => _Body(session: s, isArabic: isArabic),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  final SessionDetailModel session;
  final bool isArabic;

  const _Body({required this.session, required this.isArabic});

  String _trainerName(WidgetRef ref) {
    final memberships =
        ref.read(authProvider).valueOrNull?.user?.gymMemberships ?? const [];
    final match = memberships.firstWhere(
      (m) => m.assignedTrainerId == session.trainerId,
      orElse: () => const GymMembershipModel(gymId: ''),
    );
    return (match.assignedTrainerName ?? '').trim().isEmpty
        ? (isArabic ? 'مدرب' : 'Trainer')
        : match.assignedTrainerName!;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dateFmt = DateFormat(
        isArabic ? 'EEEE، dd MMMM yyyy' : 'EEEE, MMM dd, yyyy',
        isArabic ? 'ar' : 'en');
    final timeFmt = DateFormat('HH:mm');
    final policy = ref.watch(gymSessionPolicyProvider).valueOrNull;
    final windowHours = policy?.cancellationWindowHours ?? 24;
    final canCancel = _canCancelNow(windowHours);
    final pastWindow = _statusCancellable(session.status) && !canCancel;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _Row(
            label: isArabic ? 'المدرب' : 'Trainer',
            value: _trainerName(ref)),
        _Row(
            label: isArabic ? 'التاريخ' : 'Date',
            value: dateFmt.format(session.scheduledAt)),
        _Row(
            label: isArabic ? 'الوقت' : 'Time',
            value: timeFmt.format(session.scheduledAt)),
        _Row(
            label: isArabic ? 'المدة' : 'Duration',
            value:
                '${session.durationMinutes} ${isArabic ? 'دقيقة' : 'min'}'),
        _Row(
            label: isArabic ? 'النوع' : 'Type',
            value: sessionTypeLabel(session.sessionType, isArabic)),
        _Row(
            label: isArabic ? 'الحالة' : 'Status',
            value: sessionStatusLabel(session.status, isArabic)),
        if (session.focus != null && session.focus!.isNotEmpty)
          _Row(
              label: isArabic ? 'التركيز' : 'Focus', value: session.focus!),
        if (session.exerciseLogs.isNotEmpty) ...[
          const SizedBox(height: 24),
          Text(
            isArabic ? 'التمارين' : 'Exercises',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          ...(() {
            final sorted = [...session.exerciseLogs]
              ..sort((a, b) => a.orderIndex.compareTo(b.orderIndex));
            return sorted
                .map((log) => _ExerciseLogTile(log: log, isArabic: isArabic));
          })(),
        ],
        if (session.trainerLog != null) ...[
          const SizedBox(height: 24),
          Text(
            isArabic ? 'ملاحظات المدرب' : 'Trainer Notes',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          _TrainerLogCard(log: session.trainerLog!, isArabic: isArabic),
        ],
        const SizedBox(height: 16),
        if (canCancel)
          OutlinedButton.icon(
            onPressed: () => _cancel(context, ref),
            icon: const Icon(Icons.cancel_outlined),
            label: Text(isArabic ? 'إلغاء الحجز' : 'Cancel booking'),
            style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
          ),
        if (pastWindow)
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              isArabic
                  ? 'لا يمكن إلغاء الجلسة إلا قبل $windowHours ساعة من موعدها'
                  : 'Sessions can only be cancelled at least $windowHours hours before their scheduled time',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
        if (session.status.toLowerCase() == 'completed') ...[
          FilledButton.icon(
            onPressed: session.memberLog != null
                ? null
                : () => context.push(AppRoutes.sessionFeedback
                    .replaceFirst(':sessionId', session.id)),
            icon: const Icon(Icons.rate_review_outlined),
            label: Text(session.memberLog != null
                ? (isArabic ? 'تم التقييم' : 'Already rated')
                : (isArabic ? 'أضف تقييمك' : 'Add feedback')),
          ),
          if (session.memberLog != null) ...[
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.primaryContainer,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                isArabic
                    ? 'تم تسجيل تقييمك مسبقاً ولا يمكن تعديله'
                    : 'You already submitted feedback for this session and it cannot be changed',
              ),
            ),
          ],
        ],
      ],
    );
  }

  bool _statusCancellable(String s) {
    final v = s.toLowerCase();
    return v == 'scheduled' || v == 'confirmed' || v == 'pending';
  }

  /// True only when status is cancellable AND we are still outside the gym's
  /// cancellation window. The window comes from the per-gym session policy
  /// (default 24h, configurable by staff).
  bool _canCancelNow(int windowHours) {
    if (!_statusCancellable(session.status)) return false;
    final earliestForbidden =
        session.scheduledAt.subtract(Duration(hours: windowHours));
    return DateTime.now().isBefore(earliestForbidden);
  }

  Future<void> _cancel(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'إلغاء الحجز' : 'Cancel session'),
        content: Text(isArabic
            ? 'هل تريد إلغاء هذه الجلسة؟'
            : 'Cancel this session?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: Text(isArabic ? 'تراجع' : 'Keep')),
          FilledButton(
              style: FilledButton.styleFrom(backgroundColor: Colors.red),
              onPressed: () => Navigator.pop(ctx, true),
              child: Text(isArabic ? 'إلغاء' : 'Cancel')),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    try {
      final userId = ref.read(authProvider).valueOrNull?.user?.id;
      if (userId == null) throw Exception('Not signed in');
      final repo = ref.read(schedulingRepositoryProvider);
      await repo.cancelSession(session.id, cancelledBy: userId);
      ref.invalidate(upcomingSessionsListProvider);
      ref.invalidate(mySessionsAllProvider);
      ref.invalidate(sessionDetailProvider(session.id));
      if (context.mounted) context.pop();
    } catch (e) {
      ref.invalidate(upcomingSessionsListProvider);
      ref.invalidate(mySessionsAllProvider);
      ref.invalidate(sessionDetailProvider(session.id));
      if (context.mounted) {
        final msg = e.toString().contains('InvalidStatusTransition')
            ? (isArabic
                ? 'تعذر إلغاء الجلسة بسبب تغير حالتها'
                : 'Cannot cancel — session status has changed')
            : e.toString();
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    }
  }
}

class _ExerciseLogTile extends StatelessWidget {
  final SessionExerciseLogModel log;
  final bool isArabic;
  const _ExerciseLogTile({required this.log, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final name = isArabic && (log.exerciseNameAr?.trim().isNotEmpty ?? false)
        ? log.exerciseNameAr!
        : log.exerciseName;

    // Reps may be "10" or per-set CSV "10,8,8". Render the per-set breakdown
    // when present so trainee sees what trainer actually logged.
    final repsRaw = log.reps.trim();
    final perSet = repsRaw.contains(',')
        ? repsRaw.split(',').map((s) => s.trim()).where((s) => s.isNotEmpty).toList()
        : <String>[];

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 12,
                backgroundColor: theme.colorScheme.primary,
                child: Text(
                  '${log.orderIndex + 1}',
                  style: theme.textTheme.labelSmall
                      ?.copyWith(color: theme.colorScheme.onPrimary),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(name,
                    style: theme.textTheme.titleSmall
                        ?.copyWith(fontWeight: FontWeight.w700)),
              ),
            ],
          ),
          const SizedBox(height: 8),
          _kv(theme, isArabic ? 'المجموعات' : 'Sets', '${log.sets}'),
          _kv(
            theme,
            isArabic ? 'التكرارات' : 'Reps',
            perSet.isEmpty ? repsRaw : perSet.join(' • '),
          ),
          if (log.weight != null)
            _kv(
              theme,
              isArabic ? 'الوزن' : 'Weight',
              '${_fmtWeight(log.weight!)} ${isArabic ? 'كجم' : 'kg'}',
            ),
          if (log.notes != null && log.notes!.trim().isNotEmpty) ...[
            const SizedBox(height: 4),
            _kv(theme, isArabic ? 'ملاحظات' : 'Notes', log.notes!.trim()),
          ],
        ],
      ),
    );
  }

  Widget _kv(ThemeData theme, String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 90,
            child: Text(label,
                style: theme.textTheme.labelMedium
                    ?.copyWith(color: theme.hintColor)),
          ),
          Expanded(
            child: Text(value, style: theme.textTheme.bodyMedium),
          ),
        ],
      ),
    );
  }

  String _fmtWeight(double w) =>
      w == w.truncateToDouble() ? w.toStringAsFixed(0) : w.toStringAsFixed(1);
}

class _TrainerLogCard extends StatelessWidget {
  final SessionTrainerLogModel log;
  final bool isArabic;
  const _TrainerLogCard({required this.log, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final entries = <(String, String)>[];
    void add(String label, String? value) {
      if (value != null && value.trim().isNotEmpty) {
        entries.add((label, value.trim()));
      }
    }

    add(isArabic ? 'الملاحظات' : 'Feedback', log.feedback);
    add(isArabic ? 'الأداء الفعلي مقابل المخطط' : 'Actual vs planned',
        log.actualVsPlanned);
    add(isArabic ? 'التمارين المنفذة' : 'Exercises performed',
        log.exercisesPerformed);
    add(isArabic ? 'ملاحظات على الأداء' : 'Technique notes',
        log.techniqueNotes);
    add(isArabic ? 'نقاط التحسين' : 'Areas to improve', log.areasToImprove);
    add(isArabic ? 'محور الجلسة القادمة' : 'Next session focus',
        log.nextSessionFocus);
    if (log.clientEffortRating != null) {
      add(isArabic ? 'تقييم المجهود' : 'Effort rating',
          '${log.clientEffortRating}/10');
    }

    if (entries.isEmpty) {
      return Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: theme.colorScheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(
          isArabic
              ? 'لم يضف المدرب أي ملاحظات'
              : 'Trainer did not add any notes',
          style: theme.textTheme.bodySmall,
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (final (label, value) in entries) ...[
            Text(label,
                style: theme.textTheme.labelMedium
                    ?.copyWith(color: theme.hintColor)),
            const SizedBox(height: 2),
            Text(value, style: theme.textTheme.bodyMedium),
            const SizedBox(height: 10),
          ],
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  final String label;
  final String value;
  const _Row({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(label,
                style: Theme.of(context)
                    .textTheme
                    .labelMedium
                    ?.copyWith(color: Theme.of(context).hintColor)),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}
