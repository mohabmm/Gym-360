import 'package:flutter/material.dart';

import '../../models/body_metric_model.dart';
import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'change_chip.dart';

/// Check-ins newest first: date, who measured, what changed since the previous one, coach note.
class CheckInTimeline extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;

  /// "Coach Ahmed" / "You" / "Client" for an entry.
  final String Function(BodyMetricEntryModel entry) measuredByLabel;

  const CheckInTimeline({
    super.key,
    required this.journey,
    required this.strings,
    required this.measuredByLabel,
  });

  @override
  Widget build(BuildContext context) {
    final items = journey.timeline;
    if (items.isEmpty) return const SizedBox.shrink();
    final theme = Theme.of(context);
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(strings.checkIns, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            for (var i = 0; i < items.length; i++)
              _CheckInTile(
                item: items[i],
                strings: strings,
                who: measuredByLabel(items[i].entry),
                isLast: i == items.length - 1,
              ),
          ],
        ),
      ),
    );
  }
}

class _CheckInTile extends StatelessWidget {
  final CheckIn item;
  final ProgressJourneyStrings strings;
  final String who;
  final bool isLast;

  const _CheckInTile({required this.item, required this.strings, required this.who, required this.isLast});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final coach = ProgressJourney.measuredBy(item.entry) == MeasuredBy.coach;
    final color = coach ? theme.colorScheme.primary : theme.hintColor;
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Column(children: [
            const SizedBox(height: 4),
            Icon(coach ? Icons.circle : Icons.circle_outlined, size: 12, color: color),
            if (!isLast) Expanded(child: VerticalDivider(width: 12, thickness: 1, color: theme.dividerColor)),
          ]),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _Header(item: item, strings: strings, who: who, color: color),
                  const SizedBox(height: 6),
                  _Values(item: item, strings: strings),
                  if ((item.entry.notes ?? '').trim().isNotEmpty) ...[
                    const SizedBox(height: 6),
                    Text('${strings.noteLabel}: ${item.entry.notes!.trim()}',
                        style: theme.textTheme.bodySmall?.copyWith(fontStyle: FontStyle.italic)),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Header extends StatelessWidget {
  final CheckIn item;
  final ProgressJourneyStrings strings;
  final String who;
  final Color color;

  const _Header({required this.item, required this.strings, required this.who, required this.color});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Wrap(
      spacing: 8,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        Text(strings.date(item.date), style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w700)),
        Text(who, style: theme.textTheme.labelSmall?.copyWith(color: color, fontWeight: FontWeight.w600)),
        if (item.isDayOne)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
            decoration: BoxDecoration(
              color: theme.colorScheme.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(strings.dayOne,
                style: theme.textTheme.labelSmall?.copyWith(color: theme.colorScheme.primary)),
          ),
      ],
    );
  }
}

/// Changes since the previous check-in; the first check-in lists its values instead.
class _Values extends StatelessWidget {
  final CheckIn item;
  final ProgressJourneyStrings strings;

  const _Values({required this.item, required this.strings});

  @override
  Widget build(BuildContext context) {
    final style = Theme.of(context).textTheme.bodySmall;
    if (item.changes.isEmpty) {
      return Text(_readings(), style: style);
    }
    return Wrap(
      spacing: 8,
      runSpacing: 6,
      children: [
        for (final c in item.changes)
          Wrap(spacing: 4, runSpacing: 2, crossAxisAlignment: WrapCrossAlignment.center, children: [
            Text('${strings.metric(c.metric)} ${strings.value(c.latestValue)}', style: style),
            ChangeChip(change: c.deltaValue, tone: c.tone, strings: strings),
          ]),
      ],
    );
  }

  String _readings() => [
        for (final m in ProgressMetric.values)
          if (m.valueOf(item.entry) != null)
          '${strings.metric(m)} ${strings.value(MetricValue(m.valueOf(item.entry)!, m.unitFor(item.entry)))}',
      ].join(' · ');
}
