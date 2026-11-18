import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../membership/presentation/providers/membership_providers.dart';

bool _isArabic(BuildContext context) => Localizations.localeOf(context).languageCode == 'ar';

/// pt.trial_recap — the coach's latest post-trial plan, or null until one is sent.
final myTrialRecapProvider = FutureProvider<TrialRecapModel?>((ref) async {
  try {
    return await ref.watch(apiClientProvider).get<TrialRecapModel>(
          ApiEndpoints.myTrialRecap,
          parser: (data) => TrialRecapModel.fromJson(data as Map<String, dynamic>),
        );
  } on NotFoundException {
    return null;
  } on FeatureDisabledException {
    return null;
  }
});

/// The trainee's finished trial while they have no paid package yet; null otherwise.
final finishedTrialProvider = Provider<SessionPackageModel?>((ref) {
  final all = ref.watch(allPackagesProvider).valueOrNull ?? const <SessionPackageModel>[];
  final hasPaid = all.any((p) => !p.isTrial && p.status.toLowerCase() == 'active');
  if (hasPaid) return null;
  return all.where((p) => p.isTrial && p.status.toLowerCase() == 'exhausted').firstOrNull;
});

/// After the trial: the coach's plan (pt.trial_recap) or, without one, a "keep training" card.
/// Both carry the "I'm interested" button (pt.package_interest). Renders nothing otherwise.
class TrialConversionCard extends ConsumerWidget {
  const TrialConversionCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final flags = ref.watch(featureFlagsProvider);
    final finished = ref.watch(finishedTrialProvider);
    final recap = flags.isEnabled(FeatureFlagKeys.ptTrialRecap) ? ref.watch(myTrialRecapProvider).valueOrNull : null;
    final hasPaid = (ref.watch(activePackagesProvider).valueOrNull ?? const <SessionPackageModel>[])
        .any((p) => !p.isTrial);

    if (recap != null && !hasPaid) return TrialPlanCard(recap: recap);
    if (finished != null) return _KeepTrainingCard(trial: finished);
    return const SizedBox.shrink();
  }
}

/// "Your plan from coach X": note, recommended sessions and the interest button.
class TrialPlanCard extends StatelessWidget {
  final TrialRecapModel recap;
  const TrialPlanCard({super.key, required this.recap});

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final theme = Theme.of(context);
    final coach = recap.trainerName.isEmpty ? (isArabic ? 'مدربك' : 'your coach') : recap.trainerName;

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(children: [
            Icon(Icons.assignment_turned_in_outlined, color: theme.colorScheme.primary),
            const SizedBox(width: 8),
            Expanded(
              child: Text(isArabic ? 'خطتك من $coach' : 'Your plan from $coach',
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
            ),
          ]),
          const SizedBox(height: 8),
          Text(recap.note, style: theme.textTheme.bodyMedium),
          const SizedBox(height: 12),
          _RecommendedChip(sessions: recap.recommendedSessions, isArabic: isArabic),
          const SizedBox(height: 12),
          InterestButton(
            packageId: recap.packageId,
            label: isArabic ? 'مهتم بالباقة' : "I'm interested",
          ),
        ],
      ),
    );
  }
}

class _RecommendedChip extends StatelessWidget {
  final int sessions;
  final bool isArabic;
  const _RecommendedChip({required this.sessions, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.primary;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(16)),
      child: Text(
        isArabic ? 'الباقة المقترحة: $sessions حصة' : 'Recommended: $sessions sessions',
        style: TextStyle(color: color, fontWeight: FontWeight.w700),
      ),
    );
  }
}

class _KeepTrainingCard extends StatelessWidget {
  final SessionPackageModel trial;
  const _KeepTrainingCard({required this.trial});

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final theme = Theme.of(context);
    final coach = trial.trainerName ?? (isArabic ? 'مدربك' : 'your coach');

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(isArabic ? 'انتهت حصصك التجريبية 🎉' : 'Your free sessions are done 🎉',
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 6),
          Text(isArabic ? 'استمر مع $coach وحافظ على ما حققته.' : 'Keep going with $coach and keep your progress.',
              style: theme.textTheme.bodyMedium),
          const SizedBox(height: 12),
          InterestButton(packageId: trial.id, label: isArabic ? 'مهتم بالاستمرار' : "I'm interested"),
        ],
      ),
    );
  }
}

/// pt.package_interest — the trainee's own pending request. Every "I'm interested" / "Renew"
/// button reads it, so after one tap all of them show "request sent" (also after a restart),
/// until the desk sells the package or closes the lead.
final myPackageInterestProvider = FutureProvider<MyPackageInterest>((ref) async {
  if (!ref.watch(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptPackageInterest)) return MyPackageInterest.none;
  try {
    return await ref.watch(apiClientProvider).get<MyPackageInterest>(
          ApiEndpoints.packageInterestMine,
          parser: (data) => MyPackageInterest.fromJson(Map<String, dynamic>.from(data as Map)),
        );
  } on FeatureDisabledException {
    return MyPackageInterest.none;
  }
});

/// One tap tells the desk and coach the trainee wants a package. Renders nothing while the flag
/// is off; shows the sent request instead of the button while one is pending.
class InterestButton extends ConsumerStatefulWidget {
  final String packageId;
  final String label;
  const InterestButton({super.key, required this.packageId, required this.label});

  @override
  ConsumerState<InterestButton> createState() => _InterestButtonState();
}

class _InterestButtonState extends ConsumerState<InterestButton> {
  bool _busy = false;

  Future<void> _send() async {
    final isArabic = _isArabic(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(apiClientProvider).post<PackageInterestReceipt>(
            ApiEndpoints.packageInterest,
            data: {'sourcePackageId': widget.packageId},
            parser: (data) => PackageInterestReceipt.fromJson(data as Map<String, dynamic>),
          );
      ref.invalidate(myPackageInterestProvider);
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic ? 'تم! سيتواصل معك الاستقبال قريبًا' : 'Done! The desk will contact you soon'),
      ));
    } on FeatureDisabledException {
      // Flags refresh hides the button.
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
    if (!ref.watch(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptPackageInterest)) {
      return const SizedBox.shrink();
    }
    final mine = ref.watch(myPackageInterestProvider);
    if (mine.valueOrNull?.pending == true) return RequestSentNote(sentAt: mine.value!.createdAt);

    // Wait for the status before offering the button, so it never flashes and disappears.
    final busy = _busy || (mine.isLoading && !mine.hasValue);
    return SizedBox(
      width: double.infinity,
      child: FilledButton.icon(
        onPressed: busy ? null : _send,
        icon: busy
            ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
            : const Icon(Icons.thumb_up_alt_outlined),
        label: Text(widget.label),
      ),
    );
  }
}

/// "Request sent · 18 Nov — the desk will contact you": replaces the button while pending.
class RequestSentNote extends StatelessWidget {
  final DateTime? sentAt;
  const RequestSentNote({super.key, this.sentAt});

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic(context);
    final theme = Theme.of(context);
    final when = sentAt == null ? '' : ' · ${DateFormat('d MMM', isArabic ? 'ar' : 'en').format(sentAt!)}';
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.success.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(children: [
        const Icon(Icons.check_circle, color: AppColors.success, size: 20),
        const SizedBox(width: 8),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('${isArabic ? 'تم إرسال طلبك' : 'Request sent'}$when',
                style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w700)),
            Text(isArabic ? 'سيتواصل معك الاستقبال لإتمام الباقة' : 'The desk will contact you to set up your package',
                style: theme.textTheme.bodySmall),
          ]),
        ),
      ]),
    );
  }
}
