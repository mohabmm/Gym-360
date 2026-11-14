import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../exercise_progress_model.dart';
import '../exercise_progress_strings.dart';

/// Every time the exercise was done, newest first, with the coach's notes and targets.
Future<void> showExerciseHistorySheet(
  BuildContext context, {
  required ExerciseHistoryModel exercise,
  required ExerciseProgressStrings strings,
}) =>
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.6,
        maxChildSize: 0.9,
        builder: (context, controller) => ListView(
          controller: controller,
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
          children: [
            Text(exercise.displayName(strings.isArabic), style: Theme.of(context).textTheme.titleLarge),
            Text(strings.history, style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 12),
            for (final entry in exercise.entries.reversed) _EntryTile(entry: entry, strings: strings),
          ],
        ),
      ),
    );

class _EntryTile extends StatelessWidget {
  final ExerciseEntryModel entry;
  final ExerciseProgressStrings strings;

  const _EntryTile({required this.entry, required this.strings});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    final heading = [
      strings.session(entry.sessionNumber),
      strings.date(entry.date),
      if (entry.dayLabel != null) strings.dayLabel(entry.dayLabel),
      if (entry.isTrialSession) strings.trial,
    ].join(' · ');
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(heading, style: text.labelMedium?.copyWith(color: AppColors.grey500)),
          const SizedBox(height: 2),
          Text(strings.setLine(entry.sets, entry.reps, entry.weight), style: text.titleSmall),
          if (entry.notes?.trim().isNotEmpty ?? false)
            _Line(icon: Icons.sticky_note_2_outlined, text: '${strings.coachNote}: ${entry.notes!.trim()}'),
          if (entry.target != null)
            _Line(icon: Icons.flag_outlined, text: '${strings.nextTarget}: ${strings.target(entry.target!)}'),
          const Divider(height: 20),
        ],
      ),
    );
  }
}

class _Line extends StatelessWidget {
  final IconData icon;
  final String text;

  const _Line({required this.icon, required this.text});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 4),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 16, color: AppColors.grey500),
            const SizedBox(width: 6),
            Expanded(child: Text(text, style: Theme.of(context).textTheme.bodySmall)),
          ],
        ),
      );
}
