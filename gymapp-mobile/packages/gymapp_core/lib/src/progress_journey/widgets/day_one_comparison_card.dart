import 'package:flutter/material.dart';

import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import 'change_chip.dart';

/// Table: body part | day 1 | now | change (colored by goal). Only metrics measured twice or more.
class DayOneComparisonCard extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;

  const DayOneComparisonCard({super.key, required this.journey, required this.strings});

  @override
  Widget build(BuildContext context) {
    final changes = [
      for (final m in journey.trackedMetrics)
        if (journey.change(m) case final c?) c,
    ];
    if (changes.isEmpty) return const SizedBox.shrink();
    final theme = Theme.of(context);
    final header = theme.textTheme.labelSmall?.copyWith(color: theme.hintColor);
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(strings.dayOneVsNow, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 12),
            _row(
              Text('', style: header),
              Text(strings.dayOne, style: header),
              Text(strings.now, style: header),
              Text(strings.change, style: header),
            ),
            const Divider(height: 16),
            for (final c in changes)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: _row(
                  Text(strings.metric(c.metric), style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                  Text(strings.value(c.firstValue), style: theme.textTheme.bodySmall),
                  Text(strings.value(c.latestValue), style: theme.textTheme.bodyMedium),
                  ChangeChip(change: c.deltaValue, tone: c.tone, strings: strings),
                ),
              ),
          ],
        ),
      ),
    );
  }

  static Widget _row(Widget label, Widget first, Widget latest, Widget change) => Row(
        children: [
          Expanded(flex: 4, child: label),
          Expanded(flex: 3, child: first),
          Expanded(flex: 3, child: latest),
          Expanded(flex: 4, child: Align(alignment: AlignmentDirectional.centerEnd, child: change)),
        ],
      );
}
