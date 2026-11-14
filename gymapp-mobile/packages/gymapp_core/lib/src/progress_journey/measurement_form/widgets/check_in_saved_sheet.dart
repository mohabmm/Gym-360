import 'package:flutter/material.dart';

import '../../progress_metric.dart';
import '../../widgets/change_chip.dart';
import '../measurement_form_controller.dart';
import '../measurement_form_strings.dart';

/// After a check-in: what changed since last time, with wins first. Closes with "Done".
Future<void> showCheckInSavedSheet(
  BuildContext context, {
  required MeasurementFormStrings strings,
  required List<LiveChange> changes,
}) =>
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (_) => CheckInSavedSheet(strings: strings, changes: changes),
    );

class CheckInSavedSheet extends StatelessWidget {
  final MeasurementFormStrings strings;
  final List<LiveChange> changes;

  const CheckInSavedSheet({super.key, required this.strings, required this.changes});

  List<LiveChange> get _ordered => [
        ...changes.where((c) => c.tone == ChangeTone.good),
        ...changes.where((c) => c.tone != ChangeTone.good),
      ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final wins = changes.where((c) => c.tone == ChangeTone.good).length;
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 0, 24, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(wins > 0 ? '🎉' : '✅', textAlign: TextAlign.center, style: const TextStyle(fontSize: 44)),
            const SizedBox(height: 8),
            Text(strings.savedTitle(wins),
                textAlign: TextAlign.center,
                style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 4),
            Text(strings.savedSubtitle(changes.isEmpty),
                textAlign: TextAlign.center, style: theme.textTheme.bodyMedium),
            const SizedBox(height: 16),
            for (final c in _ordered.take(5)) _ChangeLine(change: c, strings: strings),
            const SizedBox(height: 16),
            FilledButton(onPressed: () => Navigator.of(context).pop(), child: Text(strings.done)),
          ],
        ),
      ),
    );
  }
}

class _ChangeLine extends StatelessWidget {
  final LiveChange change;
  final MeasurementFormStrings strings;

  const _ChangeLine({required this.change, required this.strings});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(children: [
          Expanded(child: Text(strings.journey.metric(change.metric))),
          ChangeChip(change: change.delta, tone: change.tone, strings: strings.journey),
        ]),
      );
}
