import 'package:flutter/material.dart';

import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'change_chip.dart';

/// Compact Home card: "Since day one · 34 days" + the weight (or first measured) change.
/// Renders nothing until there are two check-ins, so Home has no empty gap.
class SinceDayOneCard extends StatelessWidget {
  final ProgressJourney journey;
  final bool isArabic;
  final VoidCallback? onTap;
  final DateTime? now;

  const SinceDayOneCard({super.key, required this.journey, required this.isArabic, this.onTap, this.now});

  @override
  Widget build(BuildContext context) {
    final change = _headline();
    if (change == null) return const SizedBox.shrink();
    final s = ProgressJourneyStrings(isArabic);
    final theme = Theme.of(context);
    return AppCard(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            CircleAvatar(
              backgroundColor: toneColor(change.tone).withValues(alpha: 0.12),
              child: Icon(Icons.trending_up, color: toneColor(change.tone)),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(s.sinceDayOne, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
                  Text(
                    '${s.metric(change.metric)} · ${s.daysAgo(Duration(days: journey.daysSinceStart(now ?? DateTime.now())))}',
                    style: theme.textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            ChangeChip(change: change.deltaValue, tone: change.tone, strings: s, large: true),
            Icon(Directionality.of(context) == TextDirection.rtl ? Icons.chevron_left : Icons.chevron_right),
          ],
        ),
      ),
    );
  }

  MetricChange? _headline() {
    for (final m in ProgressMetric.values) {
      if (journey.change(m) case final c?) return c;
    }
    return null;
  }
}
