import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../membership/presentation/providers/membership_providers.dart';
import '../providers/scheduling_providers.dart';

/// Sessions the coach logged that wait for the trainee's confirmation (pt.dual_session_logging).
final pendingConfirmationsProvider = Provider<List<SessionModel>>((ref) {
  final all = ref.watch(mySessionsAllProvider).valueOrNull ?? const <SessionModel>[];
  return all.where((s) => s.memberConfirmation == 'Pending').toList()
    ..sort((a, b) => b.scheduledAt.compareTo(a.scheduledAt));
});

bool _isArabic(BuildContext context) => Localizations.localeOf(context).languageCode == 'ar';

/// "Confirm your session" card: one tap to confirm, or "Not me" to dispute.
/// Renders nothing when there is nothing to confirm.
class ConfirmSessionsCard extends ConsumerWidget {
  const ConfirmSessionsCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pending = ref.watch(pendingConfirmationsProvider);
    if (pending.isEmpty) return const SizedBox.shrink();
    final isArabic = _isArabic(context);
    final theme = Theme.of(context);

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(children: [
            Icon(Icons.task_alt, color: theme.colorScheme.primary),
            const SizedBox(width: 8),
            Text(isArabic ? 'أكّد حصصك' : 'Confirm your sessions',
                style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          ]),
          const SizedBox(height: 8),
          for (final s in pending.take(3)) _PendingSessionRow(session: s),
        ],
      ),
    );
  }
}

class _PendingSessionRow extends ConsumerStatefulWidget {
  final SessionModel session;
  const _PendingSessionRow({required this.session});

  @override
  ConsumerState<_PendingSessionRow> createState() => _PendingSessionRowState();
}

class _PendingSessionRowState extends ConsumerState<_PendingSessionRow> {
  bool _busy = false;

  Future<void> _respond(bool confirmed) async {
    final isArabic = _isArabic(context);
    final messenger = ScaffoldMessenger.of(context);
    String? reason;
    if (!confirmed) {
      reason = await _askReason(context, isArabic);
      if (reason == null || !mounted) return; // cancelled
    }
    setState(() => _busy = true);
    try {
      final api = ref.read(apiClientProvider);
      if (confirmed) {
        await api.postVoid(ApiEndpoints.sessionMemberConfirm(widget.session.id));
      } else {
        await api.postVoid(ApiEndpoints.sessionMemberDispute(widget.session.id), data: {'reason': reason});
      }
      ref.invalidate(mySessionsAllProvider);
      ref.invalidate(activePackagesProvider);
      messenger.showSnackBar(SnackBar(
        content: Text(confirmed
            ? (isArabic ? 'تم تأكيد الحصة' : 'Session confirmed')
            : (isArabic ? 'تم إبلاغ الاستقبال' : 'The desk has been told')),
      ));
    } on FeatureDisabledException {
      // Flags refresh hides this card; nothing to show.
    } catch (_) {
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic ? 'تعذّر الإرسال، حاول مرة أخرى' : 'Could not send, please try again'),
      ));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final date = DateFormat('EEE d MMM · HH:mm', isArabic ? 'ar' : 'en').format(widget.session.scheduledAt);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(child: Text(date, style: Theme.of(context).textTheme.bodyMedium)),
          TextButton(
            onPressed: _busy ? null : () => _respond(false),
            child: Text(isArabic ? 'ليست لي' : 'Not me'),
          ),
          const SizedBox(width: 4),
          FilledButton(
            onPressed: _busy ? null : () => _respond(true),
            child: Text(isArabic ? 'تأكيد' : 'Confirm'),
          ),
        ],
      ),
    );
  }
}

Future<String?> _askReason(BuildContext context, bool isArabic) {
  final controller = TextEditingController();
  return showDialog<String>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(isArabic ? 'لم تحضر هذه الحصة؟' : "Didn't attend this session?"),
      content: TextField(
        controller: controller,
        maxLength: 200,
        decoration: InputDecoration(hintText: isArabic ? 'السبب (اختياري)' : 'Reason (optional)'),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx), child: Text(isArabic ? 'إلغاء' : 'Cancel')),
        FilledButton(
          onPressed: () => Navigator.pop(ctx, controller.text.trim()),
          child: Text(isArabic ? 'إرسال' : 'Send'),
        ),
      ],
    ),
  );
}

/// Remaining sessions per active package (pt.packages), with a trial badge and a
/// renew hint at 3 or fewer sessions left. Renders nothing without packages.
class PackageSummaryCard extends ConsumerWidget {
  const PackageSummaryCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final packages = (ref.watch(activePackagesProvider).valueOrNull ?? const <SessionPackageModel>[])
        .where((p) => p.status.toLowerCase() == 'active')
        .toList();
    if (packages.isEmpty) return const SizedBox.shrink();
    return Column(children: [for (final p in packages) _PackageRemaining(package: p)]);
  }
}

class _PackageRemaining extends StatelessWidget {
  final SessionPackageModel package;
  const _PackageRemaining({required this.package});

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final theme = Theme.of(context);
    final total = package.totalSessions == 0 ? 1 : package.totalSessions;
    final left = package.remainingSessions;
    final low = !package.isTrial && left <= 3;

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Expanded(
                child: Text(
                  package.trainerName ?? (isArabic ? 'باقة التدريب' : 'Training package'),
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                ),
              ),
              if (package.isTrial) _Badge(text: isArabic ? 'تجربة مجانية' : 'Free trial', color: AppColors.info),
            ]),
            const SizedBox(height: 8),
            Text(
              isArabic ? 'متبقي $left من ${package.totalSessions} حصص' : '$left of ${package.totalSessions} sessions left',
              style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: LinearProgressIndicator(value: left / total, minHeight: 8),
            ),
            if (low) ...[
              const SizedBox(height: 8),
              Text(
                isArabic ? 'اقتربت باقتك على الانتهاء، جدّدها من الاستقبال.' : 'Your package is almost used up — renew at the desk.',
                style: theme.textTheme.bodySmall?.copyWith(color: AppColors.warning),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  final String text;
  final Color color;
  const _Badge({required this.text, required this.color});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(12)),
        child: Text(text, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
      );
}

/// Localized label for a session's trainee-confirmation state.
String confirmationLabel(String state, bool isArabic) => switch (state) {
      'Pending' => isArabic ? 'بانتظار تأكيدك' : 'Waiting for you',
      'Confirmed' => isArabic ? 'مؤكدة' : 'Confirmed',
      'Disputed' => isArabic ? 'قيد المراجعة' : 'Under review',
      'Reversed' => isArabic ? 'ملغاة' : 'Reversed',
      _ => '',
    };
