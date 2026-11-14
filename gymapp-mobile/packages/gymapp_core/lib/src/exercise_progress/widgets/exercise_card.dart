import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../exercise_insights.dart';
import '../exercise_progress_model.dart';
import '../exercise_progress_strings.dart';
import 'exercise_history_sheet.dart';
import 'exercise_sparkline.dart';

/// One exercise: latest numbers, change for the goal, badges, next target and a trend line.
class ExerciseCard extends StatelessWidget {
  final ExerciseHistoryModel exercise;
  final ExerciseLead lead;
  final ExerciseProgressStrings strings;

  const ExerciseCard({super.key, required this.exercise, required this.lead, required this.strings});

  @override
  Widget build(BuildContext context) {
    final latest = exercise.latest;
    if (latest == null) return const SizedBox.shrink();
    final change = LeadChange.of(exercise, lead);
    final text = Theme.of(context).textTheme;
    return AppCard(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      onTap: () => showExerciseHistorySheet(context, exercise: exercise, strings: strings),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(exercise.displayName(strings.isArabic), style: text.titleMedium)),
              if (exercise.trend.latestIsPersonalBest) _Badge(strings.personalBest, AppColors.accentGold),
            ],
          ),
          const SizedBox(height: 4),
          Text(strings.setLine(latest.sets, latest.reps, latest.weight), style: text.bodyMedium),
          if (change.isComparable) _ChangeLine(change: change, lead: lead, strings: strings),
          const SizedBox(height: 8),
          ExerciseSparkline(
            values: exercise.entries.map(lead.of).whereType<double>().toList(),
            color: change.improved ? AppColors.success : AppColors.grey500,
          ),
          _TargetRow(exercise: exercise, strings: strings),
        ],
      ),
    );
  }
}

class _ChangeLine extends StatelessWidget {
  final LeadChange change;
  final ExerciseLead lead;
  final ExerciseProgressStrings strings;

  const _ChangeLine({required this.change, required this.lead, required this.strings});

  @override
  Widget build(BuildContext context) {
    final color = change.improved ? AppColors.success : AppColors.grey600;
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Text(
        '${strings.leadName(lead)}: ${strings.change(change)}',
        style: Theme.of(context).textTheme.labelMedium?.copyWith(color: color, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class _TargetRow extends StatelessWidget {
  final ExerciseHistoryModel exercise;
  final ExerciseProgressStrings strings;

  const _TargetRow({required this.exercise, required this.strings});

  @override
  Widget build(BuildContext context) {
    final target = exercise.nextTarget;
    final hit = exercise.trend.hitPreviousTarget;
    if (target == null && hit == null) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(top: 8),
      child: Wrap(
        spacing: 8,
        runSpacing: 6,
        children: [
          if (hit != null)
            _Badge(hit ? strings.targetHit : strings.targetMissed, hit ? AppColors.success : AppColors.warning),
          if (target != null) _Badge('${strings.nextTarget}: ${strings.target(target)}', AppColors.info),
        ],
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  final String label;
  final Color color;

  const _Badge(this.label, this.color);

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
        child: Text(
          label,
          style: Theme.of(context).textTheme.labelSmall?.copyWith(color: color, fontWeight: FontWeight.w700),
        ),
      );
}
