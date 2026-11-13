import 'package:flutter/material.dart';

import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'change_chip.dart';

/// "Since day one": start date, days since, start → now weight, goal and target progress.
class JourneyHeroCard extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final DateTime now;
  final VoidCallback? onEditGoal;

  const JourneyHeroCard({
    super.key,
    required this.journey,
    required this.strings,
    required this.now,
    this.onEditGoal,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final start = journey.startDate!;
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.flag_outlined, color: theme.colorScheme.primary),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(strings.sinceDayOne,
                      style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
                ),
                _GoalChip(journey: journey, strings: strings, onTap: onEditGoal),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              '${strings.dayOneOn(start)} · ${strings.daysAgo(Duration(days: journey.daysSinceStart(now)))}',
              style: theme.textTheme.bodySmall,
            ),
            if (journey.baselineIsSelfReported) ...[
              const SizedBox(height: 4),
              Text(strings.selfReportedBaseline,
                  style: theme.textTheme.bodySmall?.copyWith(fontStyle: FontStyle.italic)),
            ],
            const SizedBox(height: 16),
            _Headline(journey: journey, strings: strings),
            if (journey.targetProgress case final progress?) ...[
              const SizedBox(height: 16),
              _TargetBar(progress: progress, target: journey.goal!.targetWeight!, strings: strings),
            ],
          ],
        ),
      ),
    );
  }
}

/// Weight start → now with the change, or the "baseline set" message for a single check-in.
class _Headline extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;

  const _Headline({required this.journey, required this.strings});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final headline = _headlineMetric();
    if (headline == null) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(strings.baselineSet, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
          Text(strings.baselineSetHint, style: theme.textTheme.bodySmall),
        ],
      );
    }
    // Start → now shrinks on narrow screens (long Arabic units); the change chip keeps its size.
    return Row(
      children: [
        Expanded(
          child: FittedBox(
            fit: BoxFit.scaleDown,
            alignment: AlignmentDirectional.centerStart,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                _Value(label: strings.start, value: strings.value(headline.firstValue)),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                  child: Icon(Directionality.of(context) == TextDirection.rtl ? Icons.arrow_back : Icons.arrow_forward,
                      size: 18, color: theme.hintColor),
                ),
                _Value(label: strings.now, value: strings.value(headline.latestValue), emphasized: true),
              ],
            ),
          ),
        ),
        const SizedBox(width: 8),
        ChangeChip(change: headline.deltaValue, tone: headline.tone, strings: strings, large: true),
      ],
    );
  }

  /// Weight first, else the first metric that changed (e.g. only tape measurements taken).
  MetricChange? _headlineMetric() {
    for (final m in journey.trackedMetrics) {
      if (journey.change(m) case final c?) return c;
    }
    return null;
  }
}

class _Value extends StatelessWidget {
  final String label;
  final String value;
  final bool emphasized;

  const _Value({required this.label, required this.value, this.emphasized = false});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: text.bodySmall),
        Text(value,
            style: (emphasized ? text.headlineSmall : text.titleMedium)?.copyWith(fontWeight: FontWeight.w700)),
      ],
    );
  }
}

class _GoalChip extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final VoidCallback? onTap;

  const _GoalChip({required this.journey, required this.strings, this.onTap});

  @override
  Widget build(BuildContext context) {
    final hasGoal = journey.goalType != null;
    if (!hasGoal && onTap == null) return const SizedBox.shrink();
    return ActionChip(
      avatar: Icon(hasGoal ? Icons.track_changes : Icons.add, size: 16),
      label: Text(hasGoal ? strings.goal(journey.goalType) : strings.setGoal),
      onPressed: onTap,
      visualDensity: VisualDensity.compact,
    );
  }
}

class _TargetBar extends StatelessWidget {
  final double progress;
  final double target;
  final ProgressJourneyStrings strings;

  const _TargetBar({required this.progress, required this.target, required this.strings});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(6),
          child: LinearProgressIndicator(value: progress, minHeight: 8),
        ),
        const SizedBox(height: 6),
        Text(
          strings.towardTarget(progress, MetricValue(target, MetricValue.kg)),
          style: theme.textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600),
        ),
      ],
    );
  }
}
