import 'package:flutter/material.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../sessions/presentation/pt/exercises/exercise_inputs.dart';
import '../data/plan_draft.dart';
import 'trainer_plan_strings.dart';

/// Top of the builder: name, goal, how many weeks, and "same days every week" or week tabs.
class PlanBuilderHeader extends StatelessWidget {
  final PlanDraft draft;
  final TrainerPlanStrings strings;
  final int week;
  final ValueChanged<int> onWeek;
  final VoidCallback onChanged;

  const PlanBuilderHeader({
    super.key,
    required this.draft,
    required this.strings,
    required this.week,
    required this.onWeek,
    required this.onChanged,
  });

  void _update(void Function() change) {
    change();
    onChanged();
  }

  @override
  Widget build(BuildContext context) {
    final s = strings;
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      TextFormField(
        initialValue: draft.name,
        decoration: InputDecoration(labelText: s.planName),
        onChanged: (v) => _update(() => draft.name = v),
      ),
      if (s.isArabic) ...[
        const SizedBox(height: 8),
        TextFormField(
          initialValue: draft.nameAr ?? '',
          decoration: InputDecoration(labelText: s.planNameAr),
          onChanged: (v) => _update(() => draft.nameAr = v),
        ),
      ],
      const SizedBox(height: 12),
      Text(s.goalLabel, style: Theme.of(context).textTheme.labelLarge),
      Wrap(spacing: 8, children: [
        for (final g in PlanGoal.values)
          ChoiceChip(label: Text(s.goal(g)), selected: draft.goal == g, onSelected: (_) => _update(() => draft.goal = g)),
      ]),
      if (draft.goal == PlanGoal.other)
        TextFormField(
          initialValue: draft.goalText,
          decoration: InputDecoration(hintText: s.goalTextHint),
          onChanged: (v) => _update(() => draft.goalText = v),
        ),
      const SizedBox(height: 12),
      Text(s.durationLabel, style: Theme.of(context).textTheme.labelLarge),
      Row(children: [
        Expanded(
          child: SetsStepper(value: draft.durationWeeks, isArabic: s.isArabic, onChanged: (v) => _update(() => draft.setDurationWeeks(v ?? 1))),
        ),
        const SizedBox(width: 8),
        Text(s.weeks(draft.durationWeeks)),
      ]),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        title: Text(s.sameEveryWeek),
        value: draft.sameEveryWeek,
        onChanged: (on) => _update(on ? draft.copyWeekOneToAll : draft.splitWeeks),
      ),
      if (!draft.sameEveryWeek) _WeekTabs(draft: draft, strings: s, week: week, onWeek: onWeek, onCopy: () => _update(draft.copyWeekOneToAll)),
    ]);
  }
}

class _WeekTabs extends StatelessWidget {
  final PlanDraft draft;
  final TrainerPlanStrings strings;
  final int week;
  final ValueChanged<int> onWeek;
  final VoidCallback onCopy;

  const _WeekTabs({required this.draft, required this.strings, required this.week, required this.onWeek, required this.onCopy});

  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(children: [
            for (var i = 0; i < draft.weeks.length; i++)
              Padding(
                padding: const EdgeInsetsDirectional.only(end: 6),
                child: ChoiceChip(label: Text(strings.weekLabel(i + 1)), selected: week == i, onSelected: (_) => onWeek(i)),
              ),
          ]),
        ),
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: TextButton.icon(onPressed: onCopy, icon: const Icon(Icons.copy_all), label: Text(strings.copyWeekOne)),
        ),
      ]);
}
