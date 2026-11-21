import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../exercise_progress_model.dart';
import '../exercise_progress_strings.dart';
import '../session_insights.dart';
import '../session_strings.dart';
import 'session_card.dart';

/// The board by session: what the coach lined up for next time, then every session newest
/// first — the latest open, older ones folded.
class SessionTimeline extends StatelessWidget {
  final ExerciseProgressModel board;
  final ExerciseProgressStrings strings;
  final ValueChanged<String>? onOpenSession;

  const SessionTimeline({super.key, required this.board, required this.strings, this.onOpenSession});

  @override
  Widget build(BuildContext context) {
    final plan = NextSessionPlan.of(board);
    final newestFirst = board.sessions.reversed.toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (plan != null) _NextSessionCard(plan: plan, summary: board.summary, strings: strings),
        for (final (i, session) in newestFirst.indexed)
          SessionCard(
            key: ValueKey(session.sessionId),
            recap: SessionRecap.of(board, session),
            strings: strings,
            initiallyExpanded: i == 0,
            onOpenSession: onOpenSession,
          ),
      ],
    );
  }
}

/// "Your next session — Free session 3 of 3 · Your coach set 2 targets for you".
class _NextSessionCard extends StatelessWidget {
  final NextSessionPlan plan;
  final ExerciseProgressSummaryModel summary;
  final ExerciseProgressStrings strings;

  const _NextSessionCard({required this.plan, required this.summary, required this.strings});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    final free = strings.upcomingFreeSession(summary);
    return AppCard(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      color: AppColors.info.withValues(alpha: 0.08),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.flag_outlined, color: AppColors.info),
              const SizedBox(width: 8),
              Expanded(child: Text(strings.nextSession, style: text.titleMedium?.copyWith(fontWeight: FontWeight.w700))),
            ],
          ),
          if (free != null) Text(free, style: text.labelMedium?.copyWith(color: AppColors.info)),
          if (plan.focus != null) ...[
            const SizedBox(height: 8),
            Text('${strings.focus}: ${plan.focus!}', style: text.bodyMedium),
          ],
          if (plan.targets.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(strings.targetsSet(plan.targets.length), style: text.bodySmall?.copyWith(fontWeight: FontWeight.w600)),
            for (final row in plan.targets)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  '${row.exercise.displayName(strings.isArabic)} · ${strings.target(row.entry.target!)}',
                  style: text.bodyMedium,
                ),
              ),
          ],
        ],
      ),
    );
  }
}
