import 'package:flutter/material.dart';

import '../../progress_journey/progress_journey_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../../widgets/empty_state.dart';
import '../exercise_insights.dart';
import '../exercise_progress_model.dart';
import '../exercise_progress_strings.dart';
import 'exercise_card.dart';

/// pt.exercise_progress: the member's board — goal story, day tabs (Push / Pull / Legs …) and
/// one card per exercise. Shared by the trainee app (own board) and the trainer app (a client's).
class ExerciseProgressView extends StatefulWidget {
  final ExerciseProgressModel board;
  final ProgressGoalType? goal;
  final bool isArabic;

  /// Shown under the cards, e.g. the trainee's "keep training" card during the trial.
  final Widget? footer;

  /// Empty-state text; defaults to the trainee wording.
  final String? emptyBody;

  const ExerciseProgressView({
    super.key,
    required this.board,
    required this.goal,
    required this.isArabic,
    this.footer,
    this.emptyBody,
  });

  @override
  State<ExerciseProgressView> createState() => _ExerciseProgressViewState();
}

class _ExerciseProgressViewState extends State<ExerciseProgressView> {
  TrainingDayModel? _day;

  @override
  Widget build(BuildContext context) {
    final strings = ExerciseProgressStrings(widget.isArabic);
    final board = widget.board;
    if (board.isEmpty) {
      return Column(children: [
        EmptyState(icon: Icons.fitness_center, title: strings.emptyTitle, subtitle: widget.emptyBody ?? strings.emptyBody),
        if (widget.footer != null) widget.footer!,
      ]);
    }
    final lead = ExerciseLead.forGoal(widget.goal);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _InsightCard(board: board, goal: widget.goal, strings: strings),
        const SizedBox(height: 12),
        if (board.days.length > 1) _DayChips(days: board.days, selected: _day, strings: strings, onSelected: _select),
        const SizedBox(height: 8),
        for (final exercise in board.exercisesFor(_day)) ExerciseCard(exercise: exercise, lead: lead, strings: strings),
        if (widget.footer != null) widget.footer!,
      ],
    );
  }

  void _select(TrainingDayModel? day) => setState(() => _day = day);
}

class _InsightCard extends StatelessWidget {
  final ExerciseProgressModel board;
  final ProgressGoalType? goal;
  final ExerciseProgressStrings strings;

  const _InsightCard({required this.board, required this.goal, required this.strings});

  @override
  Widget build(BuildContext context) {
    final insight = BoardInsight.of(board, goal);
    final badges = strings.badges(insight);
    final text = Theme.of(context).textTheme;
    final summary = board.summary;
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
          Text(strings.trialProgress(summary), style: text.bodySmall),
          if (summary.isTrialView && summary.trialSessionsTotal > 0) ...[
            const SizedBox(height: 6),
            LinearProgressIndicator(
              value: (summary.trialSessionsUsed / summary.trialSessionsTotal).clamp(0, 1).toDouble(),
              minHeight: 6,
              borderRadius: BorderRadius.circular(3),
            ),
          ],
        ],
      ),
    );
  }
}

class _DayChips extends StatelessWidget {
  final List<TrainingDayModel> days;
  final TrainingDayModel? selected;
  final ExerciseProgressStrings strings;
  final ValueChanged<TrainingDayModel?> onSelected;

  const _DayChips({required this.days, required this.selected, required this.strings, required this.onSelected});

  @override
  Widget build(BuildContext context) => SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            _chip(strings.allDays, selected == null, () => onSelected(null)),
            for (final day in days)
              _chip('${strings.dayLabel(day.label)} · ${day.sessionsCount}', identical(selected, day), () => onSelected(day)),
          ],
        ),
      );

  Widget _chip(String label, bool isSelected, VoidCallback onTap) => Padding(
        padding: const EdgeInsetsDirectional.only(end: 8),
        child: ChoiceChip(label: Text(label), selected: isSelected, onSelected: (_) => onTap()),
      );
}
