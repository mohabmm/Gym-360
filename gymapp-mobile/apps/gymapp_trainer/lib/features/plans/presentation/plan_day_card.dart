import 'package:flutter/material.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/plan_draft.dart';
import 'plan_exercise_sheet.dart';
import 'trainer_plan_strings.dart';

/// One workout day in the builder: its name and exercises (drag to reorder, tap to edit).
class PlanDayCard extends StatelessWidget {
  final DayDraft day;
  final int index;
  final TrainerPlanStrings strings;
  final VoidCallback onChanged;
  final VoidCallback onDuplicate;
  final VoidCallback onDelete;

  const PlanDayCard({
    super.key,
    required this.day,
    required this.index,
    required this.strings,
    required this.onChanged,
    required this.onDuplicate,
    required this.onDelete,
  });

  Future<void> _edit(BuildContext context, [int? at]) async {
    final result = await showPlanExerciseSheet(context, initial: at == null ? null : day.exercises[at]);
    if (result == null) return;
    if (at == null) {
      day.exercises.add(result);
    } else {
      day.exercises[at] = result;
    }
    onChanged();
  }

  @override
  Widget build(BuildContext context) {
    final s = strings;
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 4, 4, 8),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            Expanded(
              child: TextFormField(
                key: ValueKey('day-title-${identityHashCode(day)}'),
                initialValue: s.isArabic ? (day.titleAr ?? day.title) : day.title,
                decoration: InputDecoration(labelText: s.dayTitle, hintText: s.dayHint(index)),
                onChanged: (v) {
                  if (s.isArabic) {
                    day.titleAr = v;
                    if (day.title.trim().isEmpty || day.key.isEmpty) day.title = v;
                  } else {
                    day.title = v;
                  }
                  onChanged();
                },
              ),
            ),
            PopupMenuButton<String>(
              onSelected: (v) => v == 'copy' ? onDuplicate() : onDelete(),
              itemBuilder: (_) => [
                PopupMenuItem(value: 'copy', child: Text(s.duplicateDay)),
                PopupMenuItem(value: 'delete', child: Text(s.deleteDay)),
              ],
            ),
          ]),
          ReorderableListView(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            buildDefaultDragHandles: false,
            onReorderItem: (from, to) {
              day.moveExercise(from, to);
              onChanged();
            },
            children: [
              for (var i = 0; i < day.exercises.length; i++)
                _ExerciseRow(
                  key: ObjectKey(day.exercises[i]),
                  index: i,
                  exercise: day.exercises[i].toModel(),
                  strings: s,
                  onTap: () => _edit(context, i),
                  onRemove: () {
                    day.exercises.removeAt(i);
                    onChanged();
                  },
                ),
            ],
          ),
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: TextButton.icon(onPressed: () => _edit(context), icon: const Icon(Icons.add), label: Text(s.addExercise)),
          ),
        ]),
      ),
    );
  }
}

class _ExerciseRow extends StatelessWidget {
  final int index;
  final PlanExerciseModel exercise;
  final TrainerPlanStrings strings;
  final VoidCallback onTap;
  final VoidCallback onRemove;

  const _ExerciseRow({
    super.key,
    required this.index,
    required this.exercise,
    required this.strings,
    required this.onTap,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    final rest = exercise.restSeconds;
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: ReorderableDragStartListener(index: index, child: const Icon(Icons.drag_indicator)),
      title: Text(exercise.displayName(strings.isArabic)),
      subtitle: Text([strings.prescription(exercise), if (rest != null) strings.rest(rest)].join(' · ')),
      trailing: IconButton(icon: const Icon(Icons.close), tooltip: strings.remove, onPressed: onRemove),
      onTap: onTap,
    );
  }
}
