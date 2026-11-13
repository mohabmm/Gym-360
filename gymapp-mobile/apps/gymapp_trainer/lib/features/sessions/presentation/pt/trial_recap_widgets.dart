import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../../../revenue/presentation/providers/revenue_providers.dart';

bool _isArabic(BuildContext context) => Localizations.localeOf(context).languageCode == 'ar';

/// Trials whose free sessions are used up while the client has no paid package with this coach.
final finishedTrialsProvider = Provider.autoDispose<List<SessionPackageModel>>((ref) {
  final all = ref.watch(allClientPackagesProvider).valueOrNull ?? const <SessionPackageModel>[];
  final paidMembers = all.where((p) => !p.isTrial && p.status.toLowerCase() == 'active').map((p) => p.memberId).toSet();
  return all
      .where((p) => p.isTrial && p.status.toLowerCase() == 'exhausted' && !paidMembers.contains(p.memberId))
      .toList();
});

/// pt.trial_recap — "Trials to follow up": each finished trial with a "Send plan" button.
/// Renders nothing when there are none.
class FinishedTrialsPanel extends ConsumerWidget {
  const FinishedTrialsPanel({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trials = ref.watch(finishedTrialsProvider);
    if (trials.isEmpty) return const SizedBox.shrink();
    final isArabic = _isArabic(context);
    final names = ref.watch(clientNameByIdProvider);
    final theme = Theme.of(context);

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(isArabic ? 'تجارب انتهت — أرسل الخطة' : 'Finished trials — send the plan',
              style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 4),
          Text(
            isArabic ? 'خطة قصيرة منك تزيد فرصة اشتراك العميل.' : 'A short plan from you makes the client far more likely to sign up.',
            style: theme.textTheme.bodySmall,
          ),
          const SizedBox(height: 8),
          for (final t in trials)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(children: [
                Expanded(child: Text(t.memberName ?? names[t.memberId] ?? (isArabic ? 'عميل' : 'Client'))),
                SendPlanButton(package: t, clientName: t.memberName ?? names[t.memberId] ?? ''),
              ]),
            ),
        ],
      ),
    );
  }
}

/// Opens the "Send plan" sheet for a trial. Renders nothing for paid packages or while
/// pt.trial_recap is off.
class SendPlanButton extends ConsumerWidget {
  final SessionPackageModel package;
  final String clientName;
  const SendPlanButton({super.key, required this.package, required this.clientName});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!package.isTrial || !ref.watch(isFeatureEnabledProvider(FeatureFlagKeys.ptTrialRecap))) {
      return const SizedBox.shrink();
    }
    final isArabic = _isArabic(context);
    return TextButton.icon(
      onPressed: () => showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        builder: (_) => SendPlanSheet(packageId: package.id, clientName: clientName),
      ),
      icon: const Icon(Icons.send_outlined, size: 18),
      label: Text(isArabic ? 'أرسل الخطة' : 'Send plan'),
    );
  }
}

/// Note + recommended package size (no price: the desk prices it).
class SendPlanSheet extends ConsumerStatefulWidget {
  final String packageId;
  final String clientName;
  const SendPlanSheet({super.key, required this.packageId, required this.clientName});

  @override
  ConsumerState<SendPlanSheet> createState() => _SendPlanSheetState();
}

class _SendPlanSheetState extends ConsumerState<SendPlanSheet> {
  final _note = TextEditingController();
  int _sessions = 12;
  bool _busy = false;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final isArabic = _isArabic(context);
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(apiClientProvider).postVoid(
            ApiEndpoints.trialRecap(widget.packageId),
            data: {'note': _note.text.trim(), 'recommendedSessions': _sessions},
          );
      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic ? 'تم إرسال الخطة للعميل' : 'Plan sent to the client'),
      ));
    } on FeatureDisabledException {
      navigator.pop();
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
    final theme = Theme.of(context);
    final canSend = !_busy && _note.text.trim().isNotEmpty;

    return Padding(
      padding: EdgeInsets.fromLTRB(16, 16, 16, 16 + MediaQuery.of(context).viewInsets.bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(isArabic ? 'خطة ${widget.clientName}' : "${widget.clientName}'s plan",
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 12),
          TextField(
            controller: _note,
            maxLength: 500,
            maxLines: 4,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              labelText: isArabic ? 'ملاحظاتك وخطتك' : 'Your notes and plan',
              hintText: isArabic ? 'مثال: بداية قوية، نركز على الشكل الصحيح ٣ مرات أسبوعيًا' : 'e.g. Strong start, focus on form, 3x a week',
            ),
          ),
          Text(isArabic ? 'الباقة المقترحة' : 'Recommended package', style: theme.textTheme.labelLarge),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final n in recommendableSessionCounts)
                ChoiceChip(
                  label: Text(isArabic ? '$n حصة' : '$n sessions'),
                  selected: _sessions == n,
                  onSelected: (_) => setState(() => _sessions = n),
                ),
            ],
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: FilledButton(
              onPressed: canSend ? _send : null,
              child: Text(isArabic ? 'إرسال للعميل' : 'Send to client'),
            ),
          ),
        ],
      ),
    );
  }
}
