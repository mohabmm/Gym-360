import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'change_chip.dart';

/// Changes since day one that move toward the goal, biggest relative change first.
List<MetricChange> journeyWins(ProgressJourney journey) {
  final wins = [
    for (final m in journey.trackedMetrics)
      if (journey.change(m) case final c? when c.tone == ChangeTone.good) c,
  ];
  double share(MetricChange c) => c.first == 0 ? 0 : (c.delta / c.first).abs();
  return wins..sort((a, b) => share(b).compareTo(share(a)));
}

/// "Your wins since day one": goal-aligned changes as tiles + milestone chips.
/// Renders nothing until there are at least two check-ins.
class ProgressWinsCard extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final DateTime now;

  const ProgressWinsCard({super.key, required this.journey, required this.strings, required this.now});

  @override
  Widget build(BuildContext context) {
    if (journey.isEmpty || journey.hasOnlyBaseline) return const SizedBox.shrink();
    final theme = Theme.of(context);
    final wins = journeyWins(journey).take(3).toList();
    return AppCard(
      key: const ValueKey('progress-wins'),
      padding: const EdgeInsets.all(16),
      color: AppColors.success.withValues(alpha: 0.08),
      border: Border.all(color: AppColors.success.withValues(alpha: 0.3)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(children: [
            const Text('🏆', style: TextStyle(fontSize: 22)),
            const SizedBox(width: 8),
            Expanded(
              child: Text(wins.isEmpty ? strings.keepGoing : strings.yourWins,
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
            ),
          ]),
          if (wins.isNotEmpty) ...[
            const SizedBox(height: 12),
            Row(children: [for (final w in wins) Expanded(child: _WinTile(win: w, strings: strings))]),
          ],
          const SizedBox(height: 12),
          _Milestones(journey: journey, strings: strings, now: now),
        ],
      ),
    );
  }
}

class _WinTile extends StatelessWidget {
  final MetricChange win;
  final ProgressJourneyStrings strings;

  const _WinTile({required this.win, required this.strings});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsetsDirectional.only(end: 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(strings.metric(win.metric), style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 4),
            ChangeChip(change: win.deltaValue, tone: win.tone, strings: strings, large: true),
          ],
        ),
      );
}

class _Milestones extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final DateTime now;

  const _Milestones({required this.journey, required this.strings, required this.now});

  @override
  Widget build(BuildContext context) {
    final target = journey.targetProgress;
    return Wrap(spacing: 8, runSpacing: 8, children: [
      _chip(context, Icons.event_available, strings.checkInCount(journey.fromDayOne.length)),
      _chip(context, Icons.calendar_month, strings.daysOnJourney(journey.daysSinceStart(now))),
      if (target != null && target > 0) _chip(context, Icons.flag, strings.targetDone(target)),
    ]);
  }

  Widget _chip(BuildContext context, IconData icon, String label) => Chip(
        avatar: Icon(icon, size: 16),
        label: Text(label),
        visualDensity: VisualDensity.compact,
      );
}
