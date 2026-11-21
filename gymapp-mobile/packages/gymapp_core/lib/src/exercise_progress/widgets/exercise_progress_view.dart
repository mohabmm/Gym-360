import 'package:flutter/material.dart';

import '../../progress_journey/progress_journey_model.dart';
import '../../widgets/empty_state.dart';
import '../exercise_insights.dart';
import '../exercise_progress_model.dart';
import '../exercise_progress_strings.dart';
import '../session_strings.dart';
import 'board_insight_card.dart';
import 'exercise_card.dart';
import 'session_timeline.dart';

/// How the board is read: session by session (default) or exercise by exercise.
enum ExerciseBoardTab { sessions, exercises }

/// pt.exercise_progress: the member's board — goal story, then either every session (newest
/// first, with the coach's recap) or day tabs (Push / Pull / Legs …) with one card per exercise.
/// Shared by the trainee app (own board) and the trainer app (a client's).
class ExerciseProgressView extends StatefulWidget {
  final ExerciseProgressModel board;
  final ProgressGoalType? goal;
  final bool isArabic;

  /// Shown above everything, e.g. the trainee's "keep training" card once the trial is over.
  final Widget? header;

  /// Shown under the cards, e.g. the trainee's "keep tracking" line during the trial.
  final Widget? footer;

  /// Empty-state text; defaults to the trainee wording.
  final String? emptyBody;

  final ExerciseBoardTab initialTab;

  /// Opens a session's full details; session cards show no link when null.
  final ValueChanged<String>? onOpenSession;

  const ExerciseProgressView({
    super.key,
    required this.board,
    required this.goal,
    required this.isArabic,
    this.header,
    this.footer,
    this.emptyBody,
    this.initialTab = ExerciseBoardTab.sessions,
    this.onOpenSession,
  });

  @override
  State<ExerciseProgressView> createState() => _ExerciseProgressViewState();
}

class _ExerciseProgressViewState extends State<ExerciseProgressView> {
  late ExerciseBoardTab _tab = widget.initialTab;
  TrainingDayModel? _day;

  @override
  Widget build(BuildContext context) {
    final strings = ExerciseProgressStrings(widget.isArabic);
    final board = widget.board;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (widget.header != null) widget.header!,
        if (board.isEmpty)
          EmptyState(icon: Icons.fitness_center, title: strings.emptyTitle, subtitle: widget.emptyBody ?? strings.emptyBody)
        else ...[
          BoardInsightCard(board: board, goal: widget.goal, strings: strings),
          const SizedBox(height: 12),
          _TabSwitch(tab: _tab, strings: strings, onChanged: (tab) => setState(() => _tab = tab)),
          const SizedBox(height: 12),
          if (_tab == ExerciseBoardTab.sessions)
            SessionTimeline(board: board, strings: strings, onOpenSession: widget.onOpenSession)
          else
            ..._exercises(board, strings),
        ],
        if (widget.footer != null) widget.footer!,
      ],
    );
  }

  List<Widget> _exercises(ExerciseProgressModel board, ExerciseProgressStrings strings) {
    final lead = ExerciseLead.forGoal(widget.goal);
    return [
      if (board.days.length > 1) _DayChips(days: board.days, selected: _day, strings: strings, onSelected: _select),
      const SizedBox(height: 8),
      for (final exercise in board.exercisesFor(_day)) ExerciseCard(exercise: exercise, lead: lead, strings: strings),
    ];
  }

  void _select(TrainingDayModel? day) => setState(() => _day = day);
}

class _TabSwitch extends StatelessWidget {
  final ExerciseBoardTab tab;
  final ExerciseProgressStrings strings;
  final ValueChanged<ExerciseBoardTab> onChanged;

  const _TabSwitch({required this.tab, required this.strings, required this.onChanged});

  @override
  Widget build(BuildContext context) => SegmentedButton<ExerciseBoardTab>(
        showSelectedIcon: false,
        segments: [
          ButtonSegment(
            value: ExerciseBoardTab.sessions,
            icon: const Icon(Icons.event_note_outlined),
            label: Text(strings.tabSessions),
          ),
          ButtonSegment(
            value: ExerciseBoardTab.exercises,
            icon: const Icon(Icons.fitness_center),
            label: Text(strings.tabExercises),
          ),
        ],
        selected: {tab},
        onSelectionChanged: (selection) => onChanged(selection.first),
      );
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
