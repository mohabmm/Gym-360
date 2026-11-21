import 'package:flutter/material.dart';

import '../../progress_journey/progress_journey_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../exercise_insights.dart';
import '../exercise_progress_model.dart';
import '../exercise_progress_strings.dart';
import '../session_strings.dart';

/// Top of the board: the goal story ("Stronger on 3 of 4 exercises"), wins, and trial progress.
class BoardInsightCard extends StatelessWidget {
  final ExerciseProgressModel board;
  final ProgressGoalType? goal;
  final ExerciseProgressStrings strings;

  const BoardInsightCard({super.key, required this.board, required this.goal, required this.strings});

  @override
  Widget build(BuildContext context) {
    final insight = BoardInsight.of(board, goal);
    final badges = strings.badges(insight);
    final text = Theme.of(context).textTheme;
    return AppCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(strings.goalFocus(goal), style: text.labelMedium?.copyWith(color: AppColors.grey500)),
          const SizedBox(height: 6),
          Text(strings.headline(insight), style: text.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          if (badges.isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(badges, style: text.bodySmall?.copyWith(color: AppColors.success, fontWeight: FontWeight.w600)),
          ],
          const SizedBox(height: 12),
          _TrialProgress(summary: board.summary, strings: strings),
        ],
      ),
    );
  }
}

/// Short trials as one dot per free session ("● ● ○  1 free session left"); long ones as a bar.
class _TrialProgress extends StatelessWidget {
  static const _maxDots = 8;

  final ExerciseProgressSummaryModel summary;
  final ExerciseProgressStrings strings;

  const _TrialProgress({required this.summary, required this.strings});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme.bodySmall;
    final total = summary.trialSessionsTotal;
    if (!summary.isTrialView || total == 0) return Text(strings.trialProgress(summary), style: text);
    if (total > _maxDots) return _bar(text);
    return Row(
      children: [
        for (var i = 0; i < total; i++) _Dot(filled: i < summary.trialSessionsUsed),
        const SizedBox(width: 6),
        Expanded(child: Text(strings.freeLeft(summary), style: text?.copyWith(fontWeight: FontWeight.w600))),
      ],
    );
  }

  Widget _bar(TextStyle? text) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(strings.trialProgress(summary), style: text),
          const SizedBox(height: 6),
          LinearProgressIndicator(
            value: (summary.trialSessionsUsed / summary.trialSessionsTotal).clamp(0, 1).toDouble(),
            minHeight: 6,
            borderRadius: BorderRadius.circular(3),
          ),
        ],
      );
}

class _Dot extends StatelessWidget {
  final bool filled;

  const _Dot({required this.filled});

  @override
  Widget build(BuildContext context) => Container(
        width: 12,
        height: 12,
        margin: const EdgeInsetsDirectional.only(end: 4),
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: filled ? AppColors.primary : Colors.transparent,
          border: Border.all(color: AppColors.primary, width: 1.5),
        ),
      );
}
