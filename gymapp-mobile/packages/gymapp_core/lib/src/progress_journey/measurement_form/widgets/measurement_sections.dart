import 'package:flutter/material.dart';

import '../../../widgets/app_card.dart';
import '../../progress_metric.dart';
import '../measurement_form_controller.dart';
import '../measurement_form_strings.dart';
import '../measurement_rules.dart';
import 'metric_input_field.dart';

/// Next field in keyboard order; null after the last one.
ProgressMetric? _after(ProgressMetric m) {
  final i = checkInMetrics.indexOf(m);
  return i + 1 < checkInMetrics.length ? checkInMetrics[i + 1] : null;
}

/// Card with an icon + title, used for "Body" and "Tape measurements".
class MeasurementSectionCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? subtitle;
  final List<Widget> children;

  const MeasurementSectionCard({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    required this.children,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AppCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(children: [
            Icon(icon, size: 20, color: theme.colorScheme.primary),
            const SizedBox(width: 8),
            Expanded(child: Text(title, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700))),
            if (subtitle != null) Text(subtitle!, style: theme.textTheme.bodySmall),
          ]),
          const SizedBox(height: 12),
          for (final child in children) Padding(padding: const EdgeInsets.only(bottom: 12), child: child),
        ],
      ),
    );
  }
}

/// Weight and body fat side by side.
class BodySection extends StatelessWidget {
  final MeasurementFormController form;
  final MeasurementFormStrings strings;

  const BodySection({super.key, required this.form, required this.strings});

  @override
  Widget build(BuildContext context) => MeasurementSectionCard(
        icon: Icons.monitor_weight_outlined,
        title: strings.body,
        children: [
          _PairRow(children: [
            for (final m in checkInBodyMetrics) MetricInputField(metric: m, form: form, strings: strings, next: _after(m)),
          ]),
        ],
      );
}

/// Chest, waist, hips, neck, then left/right pairs under one group title.
class TapeSection extends StatelessWidget {
  final MeasurementFormController form;
  final MeasurementFormStrings strings;

  const TapeSection({super.key, required this.form, required this.strings});

  @override
  Widget build(BuildContext context) => MeasurementSectionCard(
        icon: Icons.straighten,
        title: strings.tape,
        subtitle: strings.tapeHint,
        children: [for (final row in checkInTapeRows) row.length == 1 ? _single(row.first) : _pair(context, row)],
      );

  Widget _single(ProgressMetric m) => MetricInputField(metric: m, form: form, strings: strings, next: _after(m));

  Widget _pair(BuildContext context, List<ProgressMetric> row) {
    final group = MetricGroup.values.firstWhere((g) => g.metrics.first == row.first);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(strings.journey.group(group), style: Theme.of(context).textTheme.labelLarge),
        const SizedBox(height: 6),
        _PairRow(children: [
          MetricInputField(metric: row[0], form: form, strings: strings, label: strings.left, next: _after(row[0])),
          MetricInputField(metric: row[1], form: form, strings: strings, label: strings.right, next: _after(row[1])),
        ]),
      ],
    );
  }
}

class _PairRow extends StatelessWidget {
  final List<Widget> children;

  const _PairRow({required this.children});

  @override
  Widget build(BuildContext context) => Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: children[0]),
          const SizedBox(width: 12),
          Expanded(child: children[1]),
        ],
      );
}
