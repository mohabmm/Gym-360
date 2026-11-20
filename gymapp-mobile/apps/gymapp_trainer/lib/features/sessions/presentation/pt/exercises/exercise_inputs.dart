import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'session_exercises_draft.dart';

/// Whole reps (up to 3 digits); Arabic-Indic digits are converted as typed.
class RepsInputFormatter extends TextInputFormatter {
  const RepsInputFormatter();

  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final text = ExerciseRowDraft.westernDigits(newValue.text);
    if (!RegExp(r'^\d{0,3}$').hasMatch(text)) return oldValue;
    return TextEditingValue(text: text, selection: TextSelection.collapsed(offset: text.length));
  }
}

/// Kilograms: up to 3 digits and 2 decimals ("22.5", "٢٢٫٥", "22,5").
class WeightInputFormatter extends TextInputFormatter {
  const WeightInputFormatter();

  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final text = normalizeNumber(newValue.text);
    if (!RegExp(r'^\d{0,3}(\.\d{0,2})?$').hasMatch(text)) return oldValue;
    return TextEditingValue(text: text, selection: TextSelection.collapsed(offset: text.length));
  }
}

/// An outlined field that writes every keystroke back through [onChanged].
class ExerciseTextField extends StatelessWidget {
  final String label;
  final String initial;
  final ValueChanged<String> onChanged;
  final String? errorText;
  final TextInputType keyboardType;
  final List<TextInputFormatter>? inputFormatters;
  final TextAlign textAlign;

  const ExerciseTextField({
    super.key,
    required this.label,
    required this.initial,
    required this.onChanged,
    this.errorText,
    this.keyboardType = TextInputType.text,
    this.inputFormatters,
    this.textAlign = TextAlign.start,
  });

  @override
  Widget build(BuildContext context) => TextFormField(
        initialValue: initial,
        onChanged: onChanged,
        keyboardType: keyboardType,
        inputFormatters: inputFormatters,
        textAlign: textAlign,
        textInputAction: TextInputAction.next,
        decoration: InputDecoration(
          labelText: label,
          errorText: errorText,
          isDense: true,
          border: const OutlineInputBorder(),
        ),
      );
}

/// Kilograms field: one per set, and one for the next-session target.
class WeightField extends StatelessWidget {
  final double? weight;
  final bool isArabic;
  final ValueChanged<double?> onChanged;
  final String? label;

  /// The set this weight belongs to, so each field has its own label ("Set 2 kg").
  final int? semanticSet;

  const WeightField({
    super.key,
    required this.weight,
    required this.isArabic,
    required this.onChanged,
    this.label,
    this.semanticSet,
  });

  String get _label {
    if (label != null) return label!;
    final n = semanticSet;
    if (n == null) return isArabic ? 'الوزن (كجم)' : 'Weight (kg)';
    return isArabic ? 'كجم م$n' : 'Set $n kg';
  }

  @override
  Widget build(BuildContext context) => ExerciseTextField(
        label: _label,
        initial: weight == null ? '' : _plain(weight!),
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        inputFormatters: const [WeightInputFormatter()],
        textAlign: TextAlign.center,
        onChanged: (v) => onChanged(double.tryParse(v)),
      );

  static String _plain(double v) => v == v.truncateToDouble() ? v.toStringAsFixed(0) : v.toString();
}

/// "Sets  [−] 3 [+]" — the count is always readable and one tap adds the set the member just did.
/// A null [value] (an unset target) shows "—"; [+] then starts from [startAt].
class SetsStepper extends StatelessWidget {
  final int? value;
  final bool isArabic;
  final ValueChanged<int?> onChanged;
  final bool nullable;
  final int startAt;

  const SetsStepper({
    super.key,
    required this.value,
    required this.isArabic,
    required this.onChanged,
    this.nullable = false,
    this.startAt = ExerciseRowDraft.defaultSets,
  });

  void _step(int delta) {
    final current = value;
    if (current == null) return onChanged(startAt);
    final next = current + delta;
    if (next < 1) return onChanged(nullable ? null : 1);
    onChanged(next.clamp(1, ExerciseRowDraft.maxSets));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final current = value;
    final canRemove = current != null && (current > 1 || nullable);
    final canAdd = current == null || current < ExerciseRowDraft.maxSets;
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: theme.colorScheme.outline),
        borderRadius: BorderRadius.circular(4),
      ),
      padding: const EdgeInsetsDirectional.only(start: 12),
      child: Row(
        children: [
          Expanded(child: Text(isArabic ? 'المجموعات' : 'Sets', style: theme.textTheme.labelLarge)),
          IconButton(
            visualDensity: VisualDensity.compact,
            tooltip: isArabic ? 'مجموعة أقل' : 'One set less',
            icon: const Icon(Icons.remove_circle_outline),
            onPressed: canRemove ? () => _step(-1) : null,
          ),
          SizedBox(
            width: 28,
            child: Text(current?.toString() ?? '—', textAlign: TextAlign.center, style: theme.textTheme.titleMedium),
          ),
          IconButton(
            visualDensity: VisualDensity.compact,
            tooltip: isArabic ? 'مجموعة أخرى' : 'One more set',
            icon: const Icon(Icons.add_circle_outline),
            onPressed: canAdd ? () => _step(1) : null,
          ),
        ],
      ),
    );
  }
}

/// Time held: seconds ("45") or minutes:seconds ("1:30"); Arabic digits converted as typed.
class TimeInputFormatter extends TextInputFormatter {
  const TimeInputFormatter();

  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final text = ExerciseRowDraft.westernDigits(newValue.text);
    if (!RegExp(r'^\d{0,3}(:\d{0,2})?$').hasMatch(text)) return oldValue;
    return TextEditingValue(text: text, selection: TextSelection.collapsed(offset: text.length));
  }
}

/// "عدات | وقت" — whether the exercise is counted in reps or in seconds held (plank).
class MeasureToggle extends StatelessWidget {
  final ExerciseMeasure value;
  final bool isArabic;
  final ValueChanged<ExerciseMeasure> onChanged;

  const MeasureToggle({super.key, required this.value, required this.isArabic, required this.onChanged});

  @override
  Widget build(BuildContext context) => SegmentedButton<ExerciseMeasure>(
        showSelectedIcon: false,
        style: const ButtonStyle(visualDensity: VisualDensity.compact),
        segments: [
          ButtonSegment(
            value: ExerciseMeasure.reps,
            icon: const Icon(Icons.repeat, size: 18),
            label: Text(isArabic ? 'عدات' : 'Reps'),
          ),
          ButtonSegment(
            value: ExerciseMeasure.time,
            icon: const Icon(Icons.timer_outlined, size: 18),
            label: Text(isArabic ? 'وقت' : 'Time'),
          ),
        ],
        selected: {value},
        onSelectionChanged: (s) => onChanged(s.first),
      );
}

/// One line per set: "م1  [12] عدة  [60] كجم", or for a timed exercise "م1  [0:45]  [كجم]".
class SetsList extends StatelessWidget {
  final List<SetDraft> sets;
  final ExerciseMeasure measure;
  final bool isArabic;
  final bool showErrors;
  final VoidCallback onChanged;

  const SetsList({
    super.key,
    required this.sets,
    required this.measure,
    required this.isArabic,
    required this.showErrors,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) => Column(
        children: [
          for (final (i, set) in sets.indexed)
            _SetLine(
              // Re-created when the type changes so the field shows the right value.
              key: ValueKey('set-$i-${measure.name}'),
              index: i,
              set: set,
              measure: measure,
              isArabic: isArabic,
              showError: showErrors && !set.isDone(measure),
              onChanged: onChanged,
            ),
        ],
      );
}

class _SetLine extends StatelessWidget {
  final int index;
  final SetDraft set;
  final ExerciseMeasure measure;
  final bool isArabic;
  final bool showError;
  final VoidCallback onChanged;

  const _SetLine({
    super.key,
    required this.index,
    required this.set,
    required this.measure,
    required this.isArabic,
    required this.showError,
    required this.onChanged,
  });

  String _t(String ar, String en) => isArabic ? ar : en;

  @override
  Widget build(BuildContext context) {
    final n = index + 1;
    final timed = measure == ExerciseMeasure.time;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 40,
            height: 44,
            child: Center(child: Text(_t('م$n', 'S$n'), style: Theme.of(context).textTheme.labelLarge)),
          ),
          Expanded(
            child: timed ? _timeField(n) : _repsField(n),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: WeightField(
              weight: set.weight,
              isArabic: isArabic,
              label: timed ? _t('كجم (اختياري)', 'kg (optional)') : null,
              semanticSet: n,
              onChanged: (v) {
                set.weight = v;
                onChanged();
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _repsField(int n) => ExerciseTextField(
        label: _t('عدات م$n', 'Set $n reps'),
        initial: set.reps,
        keyboardType: TextInputType.number,
        inputFormatters: const [RepsInputFormatter()],
        textAlign: TextAlign.center,
        errorText: showError ? _t('مطلوب', 'Required') : null,
        onChanged: (v) {
          set.reps = v;
          onChanged();
        },
      );

  Widget _timeField(int n) => ExerciseTextField(
        label: _t('وقت م$n (ث أو د:ث)', 'Set $n time (s or m:ss)'),
        initial: set.time,
        keyboardType: TextInputType.datetime,
        inputFormatters: const [TimeInputFormatter()],
        textAlign: TextAlign.center,
        errorText: !showError
            ? null
            : set.time.trim().isEmpty
                ? _t('مطلوب', 'Required')
                : _t('حتى 16:39', 'Up to 16:39'),
        onChanged: (v) {
          set.time = v;
          onChanged();
        },
      );
}
