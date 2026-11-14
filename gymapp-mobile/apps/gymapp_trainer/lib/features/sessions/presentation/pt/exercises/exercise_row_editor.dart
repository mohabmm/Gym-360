import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'session_exercises_draft.dart';

/// One exercise: name, sets × reps @ weight, note and the optional next-session target.
/// Writes straight into [draft]; [onChanged] lets the screen re-check "Save".
class ExerciseRowEditor extends StatefulWidget {
  final ExerciseRowDraft draft;
  final bool isArabic;
  final VoidCallback onChanged;
  final VoidCallback onRemove;

  const ExerciseRowEditor({
    super.key,
    required this.draft,
    required this.isArabic,
    required this.onChanged,
    required this.onRemove,
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

  @override
  Widget build(BuildContext context) {
    final d = widget.draft;
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 4, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Expanded(
                child: _Field(
                  label: _t('التمرين', 'Exercise'),
                  initial: d.name,
                  onChanged: (v) => _update((d) => d.name = v),
                ),
              ),
              IconButton(icon: const Icon(Icons.close), tooltip: _t('حذف', 'Remove'), onPressed: widget.onRemove),
            ]),
            _NumbersRow(
              sets: d.sets,
              reps: d.reps,
              weight: d.weight,
              isArabic: widget.isArabic,
              onSets: (v) => _update((d) => d.sets = v ?? 0),
              onReps: (v) => _update((d) => d.reps = v),
              onWeight: (v) => _update((d) => d.weight = v),
            ),
            _Field(label: _t('ملاحظة للمتدرب', 'Note for the member'), initial: d.notes, onChanged: (v) => _update((d) => d.notes = v)),
            _targetSection(d),
          ],
        ),
      ),
    );
  }

  Widget _targetSection(ExerciseRowDraft d) {
    if (!_showTarget) {
      return TextButton.icon(
        onPressed: () => setState(() => _showTarget = true),
        icon: const Icon(Icons.flag_outlined, size: 18),
        label: Text(_t('هدف الحصة القادمة', 'Next session target')),
      );
    }
    return Padding(
      padding: const EdgeInsets.only(top: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(_t('هدف الحصة القادمة', 'Next session target'), style: Theme.of(context).textTheme.labelMedium),
          _NumbersRow(
            sets: d.targetSets,
            reps: d.targetReps,
            weight: d.targetWeight,
            isArabic: widget.isArabic,
            onSets: (v) => _update((d) => d.targetSets = v),
            onReps: (v) => _update((d) => d.targetReps = v),
            onWeight: (v) => _update((d) => d.targetWeight = v),
          ),
        ],
      ),
    );
  }
}

class _NumbersRow extends StatelessWidget {
  final int? sets;
  final String reps;
  final double? weight;
  final bool isArabic;
  final ValueChanged<int?> onSets;
  final ValueChanged<String> onReps;
  final ValueChanged<double?> onWeight;

  const _NumbersRow({
    required this.sets,
    required this.reps,
    required this.weight,
    required this.isArabic,
    required this.onSets,
    required this.onReps,
    required this.onWeight,
  });

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Expanded(
            child: _Field(
              label: isArabic ? 'مجموعات' : 'Sets',
              initial: sets?.toString() ?? '',
              digitsOnly: true,
              onChanged: (v) => onSets(int.tryParse(v)),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 2,
            child: _Field(label: isArabic ? 'العدات (10,8,8)' : 'Reps (10,8,8)', initial: reps, onChanged: onReps),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: _Field(
              label: isArabic ? 'كجم' : 'kg',
              initial: weight == null ? '' : _plain(weight!),
              decimal: true,
              onChanged: (v) => onWeight(double.tryParse(v.replaceAll(',', '.'))),
            ),
          ),
          const SizedBox(width: 8),
        ],
      );

  static String _plain(double v) => v == v.truncateToDouble() ? v.toStringAsFixed(0) : v.toString();
}

class _Field extends StatelessWidget {
  final String label;
  final String initial;
  final ValueChanged<String> onChanged;
  final bool digitsOnly;
  final bool decimal;

  const _Field({
    required this.label,
    required this.initial,
    required this.onChanged,
    this.digitsOnly = false,
    this.decimal = false,
  });

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 8),
        child: TextFormField(
          initialValue: initial,
          onChanged: onChanged,
          keyboardType: digitsOnly || decimal ? TextInputType.numberWithOptions(decimal: decimal) : TextInputType.text,
          inputFormatters: digitsOnly ? [FilteringTextInputFormatter.digitsOnly] : null,
          decoration: InputDecoration(labelText: label, isDense: true, border: const OutlineInputBorder()),
        ),
      );
}
