import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../membership/presentation/providers/membership_providers.dart';
import '../../../scheduling/presentation/widgets/pt_conversion_widgets.dart';
import '../providers/progress_providers.dart';

/// Trial sessions left at which the card starts showing.
const int conversionSessionsLeft = 3;

/// The trial to convert: finished (no paid package), or active with few sessions left.
final conversionTrialProvider = Provider<SessionPackageModel?>((ref) {
  final finished = ref.watch(finishedTrialProvider);
  if (finished != null) return finished;
  final active = ref.watch(activePackagesProvider).valueOrNull ?? const <SessionPackageModel>[];
  if (active.any((p) => !p.isTrial)) return null;
  return active.where((p) => p.isTrial && p.remainingSessions <= conversionSessionsLeft).firstOrNull;
});

/// pt.package_interest on the Progress tab: the member's own results ("Weight ↓ 2.1 kg in 24 days")
/// next to "Continue with Coach X". Shows only while the trial ends and there is no paid package.
class ProgressConversionCard extends ConsumerWidget {
  final ProgressJourney journey;
  final DateTime? now;

  const ProgressConversionCard({super.key, required this.journey, this.now});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final trial = ref.watch(conversionTrialProvider);
    final enabled = ref.watch(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptPackageInterest);
    if (trial == null || !enabled) return const SizedBox.shrink();
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final copy = _ConversionCopy(isArabic, ProgressJourneyStrings(isArabic));
    final coach = trial.trainerName ?? ref.watch(assignedCoachProvider).name;
    final win = journeyWins(journey).firstOrNull;
    final theme = Theme.of(context);

    return AppCard(
      key: const ValueKey('progress-conversion'),
      padding: const EdgeInsets.all(16),
      border: Border.all(color: theme.colorScheme.primary.withValues(alpha: 0.4)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(win == null ? copy.momentumTitle : copy.resultsTitle,
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
          if (win != null) ...[
            const SizedBox(height: 8),
            _WinLine(win: win, days: journey.daysSinceStart(now ?? DateTime.now()), copy: copy),
          ],
          const SizedBox(height: 8),
          Text(copy.keepTraining(coach), style: theme.textTheme.bodyMedium),
          if (journey.targetProgress case final p? when p > 0 && p < 1) ...[
            const SizedBox(height: 4),
            Text(copy.toTarget(p), style: theme.textTheme.bodySmall),
          ],
          if (!trial.isFinished) ...[
            const SizedBox(height: 4),
            Text(copy.sessionsLeft(trial.remainingSessions),
                style: theme.textTheme.bodySmall?.copyWith(color: AppColors.warning, fontWeight: FontWeight.w700)),
          ],
          const SizedBox(height: 12),
          InterestButton(packageId: trial.id, label: copy.continueWith(coach)),
        ],
      ),
    );
  }
}

extension on SessionPackageModel {
  bool get isFinished => status.toLowerCase() == 'exhausted' || remainingSessions <= 0;
}

class _WinLine extends StatelessWidget {
  final MetricChange win;
  final int days;
  final _ConversionCopy copy;

  const _WinLine({required this.win, required this.days, required this.copy});

  @override
  Widget build(BuildContext context) => Row(children: [
        Text(copy.journey.metric(win.metric), style: Theme.of(context).textTheme.bodyLarge),
        const SizedBox(width: 8),
        ChangeChip(change: win.deltaValue, tone: win.tone, strings: copy.journey, large: true),
        const SizedBox(width: 8),
        Flexible(child: Text(copy.inDays(days), style: Theme.of(context).textTheme.bodySmall)),
      ]);
}

class _ConversionCopy {
  final bool isArabic;
  final ProgressJourneyStrings journey;

  const _ConversionCopy(this.isArabic, this.journey);

  String _t(String ar, String en) => isArabic ? ar : en;

  String get resultsTitle => _t('النتائج بدأت تظهر 💪', "You're getting results 💪");
  String get momentumTitle => _t('لا تفقد حماسك 🔥', "Don't lose your momentum 🔥");
  String inDays(int d) => _t('خلال $d يوم', d == 1 ? 'in 1 day' : 'in $d days');
  String keepTraining(String? coach) => coach == null || coach.isEmpty
      ? _t('استمر مع مدربك لتصل لهدفك.', 'Keep training with your coach to reach your goal.')
      : _t('استمر مع الكابتن $coach لتصل لهدفك.', 'Keep training with Coach $coach to reach your goal.');
  String toTarget(double p) =>
      _t('قطعت ${(p * 100).round()}% من الطريق لهدفك.', "You're ${(p * 100).round()}% of the way to your target.");
  String sessionsLeft(int n) =>
      _t('متبقي $n حصة مجانية', n == 1 ? '1 free session left' : '$n free sessions left');
  String continueWith(String? coach) => coach == null || coach.isEmpty
      ? _t('أكمل مع مدربك', 'Continue with my coach')
      : _t('أكمل مع الكابتن $coach', 'Continue with Coach $coach');
}
