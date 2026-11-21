import 'package:flutter/material.dart';

import 'exercise_inputs.dart';
import 'session_exercises_draft.dart';

/// One exercise as it happens on the gym floor: name, reps or time, how many sets, each set's
/// reps (or seconds), kg and the coach's note on it, an overall note and the optional next-session target. Writes straight into
/// [draft]; [onChanged] lets the screen re-check "Save". With [showErrors] the fields that
/// still block saving are marked.
class ExerciseRowEditor extends StatefulWidget {
  final ExerciseRowDraft draft;
  final bool isArabic;
  final bool showErrors;
  final VoidCallback onChanged;
  final VoidCallback onRemove;

  const ExerciseRowEditor({
    super.key,
    required this.draft,
    required this.isArabic,
    required this.onChanged,
    required this.onRemove,
    this.showErrors = false,
  });

  @override
  State<ExerciseRowEditor> createState() => _ExerciseRowEditorState();
}

class _ExerciseRowEditorState extends State<ExerciseRowEditor> {
  late bool _showTarget = widget.draft.hasTarget;

  String _t(String ar, String en) => widget.isArabic ? ar : en;

  void _update(void Function(ExerciseRowDraft d) apply) {
    apply(widget.draft);
    widget.onChanged();
  }

  bool get _flag => widget.showErrors && !widget.draft.isUntouched;

  @override
  Widget build(BuildContext context) {
    final d = widget.draft;
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      shape: _flag && !d.isValid
          ? RoundedRectangleBorder(
              side: BorderSide(color: theme.colorScheme.error),
              borderRadius: BorderRadius.circular(12),
            )
          : null,
      child: Padding(
        padding: const EdgeInsetsDirectional.fromSTEB(12, 8, 4, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Expanded(
                child: ExerciseTextField(
                  label: _t('التمرين', 'Exercise'),
                  initial: d.name,
                  errorText: _flag && d.name.trim().isEmpty ? _t('اكتب اسم التمرين', 'Name the exercise') : null,
                  onChanged: (v) => _update((d) => d.name = v),
                ),
              ),
              IconButton(icon: const Icon(Icons.close), tooltip: _t('حذف', 'Remove'), onPressed: widget.onRemove),
            ]),
            Padding(
              padding: const EdgeInsetsDirectional.only(top: 12, end: 8),
              child: _todayNumbers(d),
            ),
            Padding(
              padding: const EdgeInsetsDirectional.only(top: 12, end: 8),
              child: ExerciseTextField(
                label: _t('ملاحظة عامة على التمرين', 'Overall note on this exercise'),
                initial: d.notes,
                onChanged: (v) => _update((d) => d.notes = v),
              ),
            ),
            _targetSection(d),
          ],
        ),
      ),
    );
  }

  Widget _todayNumbers(ExerciseRowDraft d) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          MeasureToggle(
            value: d.measure,
            isArabic: widget.isArabic,
            onChanged: (v) => _update((d) => d.measure = v),
          ),
          const SizedBox(height: 8),
          SetsStepper(
            value: d.setCount,
            isArabic: widget.isArabic,
            onChanged: (v) => _update((d) => d.setCount = v ?? 1),
          ),
          const SizedBox(height: 12),
          SetsList(
            sets: d.sets,
            measure: d.measure,
            isArabic: widget.isArabic,
            showErrors: _flag,
            onChanged: widget.onChanged,
          ),
        ],
      );

  Widget _targetSection(ExerciseRowDraft d) {
    if (!_showTarget) {
      return TextButton.icon(
        onPressed: () => setState(() => _showTarget = true),
        icon: const Icon(Icons.flag_outlined, size: 18),
        label: Text(_t('هدف الحصة القادمة', 'Next session target')),
      );
    }
    return Padding(
      padding: const EdgeInsetsDirectional.only(top: 12, end: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(_t('هدف الحصة القادمة', 'Next session target'), style: Theme.of(context).textTheme.labelMedium),
          const SizedBox(height: 8),
          SetsStepper(
            value: d.targetSets,
            isArabic: widget.isArabic,
            nullable: true,
            startAt: d.setCount,
            onChanged: (v) => _update((d) => d.targetSets = v),
          ),
          const SizedBox(height: 8),
          Row(children: [
            Expanded(flex: 3, child: d.isTimed ? _targetTime(d) : _targetReps(d)),
            const SizedBox(width: 8),
            Expanded(
              flex: 2,
              child: WeightField(
                weight: d.targetWeight,
                isArabic: widget.isArabic,
                onChanged: (v) => _update((d) => d.targetWeight = v),
              ),
            ),
          ]),
        ],
      ),
    );
  }

  Widget _targetReps(ExerciseRowDraft d) => ExerciseTextField(
        key: const ValueKey('target-reps'),
        label: _t('العدات (مثال 8-12)', 'Reps (e.g. 8-12)'),
        initial: d.targetReps,
        textAlign: TextAlign.center,
        onChanged: (v) => _update((d) => d.targetReps = v),
      );

  Widget _targetTime(ExerciseRowDraft d) => ExerciseTextField(
        key: const ValueKey('target-time'),
        label: _t('الوقت (ث أو د:ث)', 'Time (s or m:ss)'),
        initial: d.targetSeconds == null ? '' : SetDraft.formatSeconds(d.targetSeconds!),
        keyboardType: TextInputType.datetime,
        inputFormatters: const [TimeInputFormatter()],
        textAlign: TextAlign.center,
        onChanged: (v) => _update((d) => d.targetSeconds = SetDraft.parseSeconds(v)),
      );
}
