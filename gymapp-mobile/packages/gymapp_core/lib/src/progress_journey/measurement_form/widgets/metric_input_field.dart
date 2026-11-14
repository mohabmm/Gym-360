import 'package:flutter/material.dart';

import '../../../theme/app_colors.dart';
import '../../progress_metric.dart';
import '../../widgets/change_chip.dart';
import '../measurement_form_controller.dart';
import '../measurement_form_strings.dart';
import '../measurement_rules.dart';

/// One number: label + unit, the last reading underneath, a live change chip, range error / warning.
class MetricInputField extends StatelessWidget {
  final ProgressMetric metric;
  final MeasurementFormController form;
  final MeasurementFormStrings strings;

  /// Overrides the metric name (e.g. "Left" inside the Arms row).
  final String? label;
  final ProgressMetric? next;

  const MetricInputField({
    super.key,
    required this.metric,
    required this.form,
    required this.strings,
    this.label,
    this.next,
  });

  String get _unit => strings.journey.unitOf(MetricValue(0, MeasurementFormController.unitOf(metric)));

  @override
  Widget build(BuildContext context) {
    final check = form.check(metric);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          key: ValueKey('metric-${metric.name}'),
          controller: form.fields[metric],
          focusNode: form.focus[metric],
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: const [MeasurementInputFormatter()],
          textInputAction: next == null ? TextInputAction.done : TextInputAction.next,
          onSubmitted: (_) => next == null ? FocusScope.of(context).unfocus() : form.focus[next]!.requestFocus(),
          decoration: InputDecoration(
            labelText: label ?? strings.journey.metric(metric),
            suffixText: _unit,
            errorText: check.outOfRange ? strings.range(metric) : null,
            helperText: check.bigChange ? strings.bigChange : null,
            helperMaxLines: 2,
            helperStyle: const TextStyle(color: AppColors.warning, fontWeight: FontWeight.w600),
            isDense: true,
          ),
        ),
        _LastRow(metric: metric, form: form, strings: strings),
      ],
    );
  }
}

class _LastRow extends StatelessWidget {
  final ProgressMetric metric;
  final MeasurementFormController form;
  final MeasurementFormStrings strings;

  const _LastRow({required this.metric, required this.form, required this.strings});

  @override
  Widget build(BuildContext context) {
    final last = form.last(metric);
    if (last == null) return const SizedBox(height: 4);
    final change = form.change(metric);
    final value = MetricValue(last.value, MeasurementFormController.unitOf(metric));
    return Padding(
      padding: const EdgeInsetsDirectional.only(top: 4, start: 4),
      child: Wrap(
        spacing: 6,
        runSpacing: 4,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          Text(strings.last(last, value),
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.grey500)),
          if (change != null) ChangeChip(change: change.delta, tone: change.tone, strings: strings.journey),
        ],
      ),
    );
  }
}
