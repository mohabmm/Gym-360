import 'package:flutter/material.dart';

import '../progress_journey_model.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';

/// What the user picked in [showGoalPicker].
class GoalSelection {
  final ProgressGoalType goal;
  final double? targetWeight;

  const GoalSelection(this.goal, this.targetWeight);
}

/// The optional target weight typed by the user: empty (no target) or 1–500 kg.
class TargetWeightInput {
  static const double max = 500;

  final String raw;
  final double? kg;

  const TargetWeightInput._(this.raw, this.kg);

  /// Accepts "78.5" and "78,5".
  factory TargetWeightInput.parse(String text) {
    final raw = text.trim().replaceAll(',', '.');
    return TargetWeightInput._(raw, double.tryParse(raw));
  }

  bool get isEmpty => raw.isEmpty;

  bool get isInRange => kg != null && kg! > 0 && kg! <= max;

  /// Empty is fine (no target); anything typed must be a weight in range.
  bool get isValid => isEmpty || isInRange;
}

/// Bottom sheet: pick a goal and an optional target weight. Returns null when dismissed.
Future<GoalSelection?> showGoalPicker(
  BuildContext context, {
  required ProgressJourneyStrings strings,
  ProgressGoalModel? current,
}) =>
    showModalBottomSheet<GoalSelection>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _GoalPicker(strings: strings, current: current),
    );

class _GoalPicker extends StatefulWidget {
  final ProgressJourneyStrings strings;
  final ProgressGoalModel? current;

  const _GoalPicker({required this.strings, this.current});

  @override
  State<_GoalPicker> createState() => _GoalPickerState();
}

class _GoalPickerState extends State<_GoalPicker> {
  static const _icons = {
    ProgressGoalType.loseFat: Icons.local_fire_department_outlined,
    ProgressGoalType.buildMuscle: Icons.fitness_center,
    ProgressGoalType.generalFitness: Icons.favorite_border,
  };

  late ProgressGoalType? _goal = widget.current?.goal;
  late final _target = TextEditingController(
    text: widget.current?.targetWeight == null ? '' : widget.current!.targetWeight!.compact,
  );
  String? _error;

  @override
  void dispose() {
    _target.dispose();
    super.dispose();
  }

  void _save() {
    final target = TargetWeightInput.parse(_target.text);
    if (!target.isValid) {
      setState(() => _error = widget.strings.invalidTarget);
      return;
    }
    Navigator.of(context).pop(GoalSelection(_goal!, target.kg));
  }

  @override
  Widget build(BuildContext context) {
    final s = widget.strings;
    return Padding(
      padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + MediaQuery.viewInsetsOf(context).bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(s.goalTitle, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 12),
          for (final g in ProgressGoalType.values)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: _GoalOption(
                icon: _icons[g]!,
                label: s.goal(g),
                selected: _goal == g,
                onTap: () => setState(() => _goal = g),
              ),
            ),
          const SizedBox(height: 8),
          TextField(
            controller: _target,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              labelText: s.targetWeight,
              helperText: s.targetOptional,
              errorText: _error,
              border: const OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 16),
          FilledButton(onPressed: _goal == null ? null : _save, child: Text(s.save)),
        ],
      ),
    );
  }
}

class _GoalOption extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _GoalOption({required this.icon, required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;
    return Material(
      color: selected ? primary.withValues(alpha: 0.08) : Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: selected ? primary : Theme.of(context).dividerColor),
      ),
      child: ListTile(
        leading: Icon(icon, color: selected ? primary : null),
        title: Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
        trailing: selected ? Icon(Icons.check_circle, color: primary) : null,
        onTap: onTap,
      ),
    );
  }
}
