import 'package:flutter/material.dart';

import '../../models/body_metric_model.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';
import 'change_chip.dart';

/// Opens the full report of one check-in: who measured, the coach's note, every reading with
/// its change since the last one and since day one, and InBody composition when scanned.
Future<void> showCheckInReport(BuildContext context, CheckInReport report) => showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      useSafeArea: true,
      builder: (_) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.75,
        minChildSize: 0.4,
        maxChildSize: 0.95,
        builder: (context, controller) => report.scrolledBy(controller),
      ),
    );

/// Body of [showCheckInReport]; scrollable.
class CheckInReport extends StatelessWidget {
  final ProgressJourney journey;
  final BodyMetricEntryModel entry;
  final ProgressJourneyStrings strings;
  final String measuredBy;
  final ScrollController? controller;

  const CheckInReport({
    super.key,
    required this.journey,
    required this.entry,
    required this.strings,
    required this.measuredBy,
    this.controller,
  });

  /// The same report, scrolled by the sheet's [controller].
  CheckInReport scrolledBy(ScrollController controller) => CheckInReport(
        journey: journey,
        entry: entry,
        strings: strings,
        measuredBy: measuredBy,
        controller: controller,
      );

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final readings = journey.readingsOf(entry);
    final note = entry.notes?.trim() ?? '';
    final isDayOne = identical(entry, journey.baseline);
    return ListView(
      controller: controller,
      padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
      children: [
        Text(strings.checkInReport, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
        const SizedBox(height: 4),
        Text('${strings.date(ProgressJourney.dateOf(entry))} · ${'${strings.measuredByPrefix} $measuredBy'}',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.hintColor)),
        if (note.isNotEmpty) ...[
          const SizedBox(height: 16),
          CoachNoteCard(note: note, label: strings.noteLabel),
        ],
        const SizedBox(height: 20),
        if (isDayOne || readings.every((r) => r.sincePrevious == null))
          Text(strings.firstCheckInValues, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
        _ReadingsTable(readings: readings, strings: strings),
        if (entry.composition case final c?) ...[
          const SizedBox(height: 20),
          _Composition(composition: c, strings: strings),
        ],
      ],
    );
  }
}

/// The coach's words, set apart so they are read first.
class CoachNoteCard extends StatelessWidget {
  final String note;
  final String label;
  final int? maxLines;

  const CoachNoteCard({super.key, required this.note, required this.label, this.maxLines});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = theme.colorScheme.primary;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(12),
        border: BorderDirectional(start: BorderSide(color: color, width: 3)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.format_quote_rounded, color: color, size: 20),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: theme.textTheme.labelMedium?.copyWith(color: color, fontWeight: FontWeight.w700)),
                const SizedBox(height: 2),
                Text(note,
                    maxLines: maxLines,
                    overflow: maxLines == null ? null : TextOverflow.ellipsis,
                    style: theme.textTheme.bodyMedium),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Metric | value | since last | since day 1.
class _ReadingsTable extends StatelessWidget {
  final List<CheckInReading> readings;
  final ProgressJourneyStrings strings;

  const _ReadingsTable({required this.readings, required this.strings});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final head = theme.textTheme.labelSmall?.copyWith(color: theme.hintColor, fontWeight: FontWeight.w600);
    final showPrevious = readings.any((r) => r.sincePrevious != null);
    final showDayOne = readings.any((r) => r.sinceDayOne != null);
    Widget changeCell(MetricChange? c) => Align(
          alignment: AlignmentDirectional.centerStart,
          child: c == null ? Text('—', style: head) : ChangeChip(change: c.deltaValue, tone: c.tone, strings: strings),
        );
    return Table(
      columnWidths: {
        0: const FlexColumnWidth(1.3),
        1: const FlexColumnWidth(1),
        if (showPrevious) 2: const FlexColumnWidth(1.1),
        if (showDayOne) (showPrevious ? 3 : 2): const FlexColumnWidth(1.1),
      },
      defaultVerticalAlignment: TableCellVerticalAlignment.middle,
      children: [
        TableRow(children: [
          _cell(Text(strings.measurement, style: head)),
          _cell(Text(strings.now, style: head)),
          if (showPrevious) _cell(Text(strings.sincePrevious, style: head)),
          if (showDayOne) _cell(Text(strings.sinceDayOneShort, style: head)),
        ]),
        for (final r in readings)
          TableRow(
            decoration:
                BoxDecoration(border: Border(top: BorderSide(color: theme.dividerColor.withValues(alpha: 0.4)))),
            children: [
              _cell(Text(strings.metric(r.metric), style: theme.textTheme.bodyMedium)),
              _cell(Text(strings.value(r.value),
                  style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w700))),
              if (showPrevious) _cell(changeCell(r.sincePrevious)),
              if (showDayOne) _cell(changeCell(r.sinceDayOne)),
            ],
          ),
      ],
    );
  }

  Widget _cell(Widget child) => Padding(padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 2), child: child);
}

class _Composition extends StatelessWidget {
  final BodyCompositionModel composition;
  final ProgressJourneyStrings strings;

  const _Composition({required this.composition, required this.strings});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final c = composition;
    final kg = MetricValue.kg;
    final items = <(String, String)>[
      if (c.skeletalMuscleMassKg case final v?)
        (strings.muscleMass, _withPercent(strings.value(MetricValue(v, kg)), c.skeletalMusclePercent)),
      if (c.bodyWaterKg case final v?)
        (strings.bodyWater, _withPercent(strings.value(MetricValue(v, kg)), c.bodyWaterPercent)),
      if (c.bmi case final v?) (strings.bmi, v.compact),
      if (c.heightCm case final v?) (strings.height, strings.value(MetricValue(v, 'cm'))),
    ];
    if (items.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(strings.bodyComposition, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final (label, value) in items)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(label, style: theme.textTheme.labelSmall),
                  Text(value, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
                ]),
              ),
          ],
        ),
      ],
    );
  }

  static String _withPercent(String value, double? percent) => percent == null ? value : '$value · ${percent.compact}%';
}
