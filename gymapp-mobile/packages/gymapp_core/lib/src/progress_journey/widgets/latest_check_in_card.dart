import 'package:flutter/material.dart';

import '../../models/body_metric_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'change_chip.dart';
import 'check_in_report_sheet.dart';

/// A check-in this recent gets a "New" badge.
const _newForDays = 3;

/// The latest check-in up front: who measured and when, what changed since the one before,
/// the coach's note, and the full report one tap away.
class LatestCheckInCard extends StatelessWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final DateTime now;
  final String Function(BodyMetricEntryModel entry) measuredByLabel;


  const LatestCheckInCard({
    super.key,
    required this.journey,
    required this.strings,
    required this.now,
    required this.measuredByLabel,
  });

  @override
  Widget build(BuildContext context) {
    final entry = journey.latest;
    if (entry == null) return const SizedBox.shrink();
    final theme = Theme.of(context);
    final readings = journey.readingsOf(entry);
    final changes = [
      for (final r in readings)
        if (r.sincePrevious case final c?) c
    ];
    final improved = changes.where((c) => c.tone == ChangeTone.good).toList();
    final note = entry.notes?.trim() ?? '';
    final age = _today.difference(_day(ProgressJourney.dateOf(entry))).inDays;
    final who = measuredByLabel(entry);
    final coach = ProgressJourney.measuredBy(entry) == MeasuredBy.coach;

    return AppCard(
      onTap: () => _openReport(context, entry, who),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Icon(coach ? Icons.verified_outlined : Icons.edit_note, color: theme.colorScheme.primary),
              const SizedBox(width: 8),
              Expanded(
                child: Text(strings.latestCheckIn,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              ),
              if (age <= _newForDays) _Pill(text: strings.newLabel, color: theme.colorScheme.primary),
            ]),
            const SizedBox(height: 4),
            Text('$who · ${strings.date(ProgressJourney.dateOf(entry))} · ${strings.daysAgo(Duration(days: age))}',
                style: theme.textTheme.bodySmall),
            if (changes.isNotEmpty) ...[
              const SizedBox(height: 12),
              if (improved.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(strings.improvedCount(improved),
                      style:
                          theme.textTheme.bodyMedium?.copyWith(color: AppColors.success, fontWeight: FontWeight.w700)),
                ),
              Wrap(spacing: 8, runSpacing: 8, children: [
                for (final c in _top(changes))
                  Wrap(spacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
                    Text('${strings.metric(c.metric)} ${strings.value(c.latestValue)}',
                        style: theme.textTheme.bodySmall),
                    ChangeChip(change: c.deltaValue, tone: c.tone, strings: strings),
                  ]),
              ]),
            ],
            if (note.isNotEmpty) ...[
              const SizedBox(height: 12),
              CoachNoteCard(note: note, label: strings.noteLabel, maxLines: 3),
            ],
            const SizedBox(height: 4),
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: TextButton.icon(
                onPressed: () => _openReport(context, entry, who),
                icon: const Icon(Icons.article_outlined, size: 18),
                label: Text(strings.viewReport),
              ),
            ),
          ],
        ),
      ),
    );
  }

  DateTime get _today => _day(now);
  static DateTime _day(DateTime d) => DateTime(d.year, d.month, d.day);

  /// Up to four changes, goal-relevant (good/bad) before neutral ones.
  static List<MetricChange> _top(List<MetricChange> changes) =>
      ([...changes]..sort((a, b) => _rank(a.tone).compareTo(_rank(b.tone)))).take(4).toList();
  static int _rank(ChangeTone t) => t == ChangeTone.neutral ? 1 : 0;

  void _openReport(BuildContext context, BodyMetricEntryModel entry, String who) =>
      showCheckInReport(
          context, CheckInReport(journey: journey, entry: entry, strings: strings, measuredBy: who));
}

class _Pill extends StatelessWidget {
  final String text;
  final Color color;
  const _Pill({required this.text, required this.color});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(10)),
        child: Text(text,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(color: Colors.white, fontWeight: FontWeight.w700)),
      );
}
